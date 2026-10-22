# Build desde la raíz del repositorio (donde está este Dockerfile):
#   docker-compose build

# ---------- Etapa 1: compilación ----------
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Restaurar primero solo con los .csproj para aprovechar la caché de capas
COPY VelsatMobile.Model/VelsatMobile.Model.csproj VelsatMobile.Model/
COPY VelsatMobile.Data/VelsatMobile.Data.csproj VelsatMobile.Data/
COPY VelsatMobile/VelsatMobile.csproj VelsatMobile/
RUN dotnet restore VelsatMobile/VelsatMobile.csproj

COPY . .
RUN dotnet publish VelsatMobile/VelsatMobile.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Etapa 2: ejecución ----------
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime

ENV TZ=America/Lima \
    ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASPNETCORE_ENVIRONMENT=Production

# curl para el HEALTHCHECK, zona horaria del sistema y usuario sin privilegios
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl tzdata \
    && rm -rf /var/lib/apt/lists/* \
    && test -f /usr/share/zoneinfo/$TZ \
    && ln -snf /usr/share/zoneinfo/$TZ /etc/localtime \
    && echo $TZ > /etc/timezone \
    && groupadd --system --gid 10001 app \
    && useradd --system --uid 10001 --gid app --no-create-home --shell /usr/sbin/nologin app

WORKDIR /app
COPY --from=build --chown=app:app /app/publish .

USER app
EXPOSE 8080

# /swagger/v1/swagger.json no toca la base de datos: solo indica que Kestrel responde
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -fsS -o /dev/null http://127.0.0.1:8080/swagger/v1/swagger.json || exit 1

ENTRYPOINT ["dotnet", "VelsatMobile.dll"]
