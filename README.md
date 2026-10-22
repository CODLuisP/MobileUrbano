# VelsatMobile (Urbano) – Backend API

API ASP.NET Core (.NET 6) para la app móvil. Usa Dapper + MySql.Data contra MySQL 8.0
(contenedor `mysql-gts`, bases `urbano` y `urb_his`).

| Dato | Valor |
|---|---|
| Contenedor | `velsat-mobile-urbano` |
| Puerto interno | `8080` |
| Puerto en el host | `127.0.0.1:3010` (solo loopback, Nginx hace proxy) |
| Red Docker | `app-net` (externa, compartida con `mysql-gts`) |
| Zona horaria | `America/Lima` |
| Dockerfile | `./Dockerfile` (build desde la raíz del repo) |

## Requisitos en el servidor

- Docker + docker-compose 1.29.2 (comando `docker-compose`, con guion).
- Red `app-net` ya creada y `mysql-gts` conectado a ella:
  `docker network inspect app-net`
- El usuario `gtsuser` con permisos sobre `urbano` y `urb_his`.

## Primera instalación

```bash
git clone <repo> velsat-mobile-urbano
cd velsat-mobile-urbano

# 1. Subir la configuración (único archivo manual, NO va en git)
#    desde tu PC:  scp VelsatMobile/appsettings.json usuario@droplet:~/velsat-mobile-urbano/VelsatMobile/
sudo chown 10001:10001 VelsatMobile/appsettings.json   # el contenedor corre como uid 10001
chmod 400 VelsatMobile/appsettings.json

# 2. Construir y levantar
docker-compose build
docker-compose up -d
```

> Si `VelsatMobile/appsettings.json` no existe al hacer `up`, Docker crea una carpeta con
> ese nombre y el servicio no arranca ("DefaultConnection string not found"). Bórrala,
> sube el archivo y vuelve a hacer `docker-compose down && docker-compose up -d`.

## Comprobar que arrancó

```bash
docker-compose ps                       # Estado "Up (healthy)"
docker logs --tail 50 velsat-mobile-urbano
curl -i http://127.0.0.1:3010/swagger/v1/swagger.json            # sin BD
curl -i http://127.0.0.1:3010/api/Server/MobileServer/prueba     # consulta la BD urbano
```

## Actualización

En este servidor `docker-compose up -d --build` falla al recrear un contenedor existente
(`KeyError: 'ContainerConfig'`, bug de docker-compose 1.29). Actualizar siempre así:

```bash
git pull && docker-compose build && docker-compose down && docker-compose up -d
```

## Configuración (`VelsatMobile/appsettings.json`)

Un solo archivo, fuera de git y de la imagen, montado en solo lectura en `/app/appsettings.json`.
Plantilla: `VelsatMobile/appsettings.json.example`.

| Clave | Descripción |
|---|---|
| `ConnectionStrings:DefaultConnection` | Base `urbano` |
| `ConnectionStrings:SecondConnection` | Base `urb_his` |
| `settings:secretkey` | Clave de firma JWT (obligatoria) |
| `Firebase:ProjectId` | `velsat-app` |
| `Firebase:Credentials` | Contenido completo del JSON de la cuenta de servicio de Firebase |
| `Logging`, `Kestrel:Limits` | Niveles de log y límites de Kestrel |

Si cambias el archivo basta con `docker-compose restart` (no hace falta reconstruir).

Cualquier clave se puede sobrescribir con una variable de entorno (tienen prioridad sobre
el archivo), p. ej. `ConnectionStrings__DefaultConnection`, añadiéndola en `environment:`
del docker-compose.yml.

Fijadas en el Dockerfile: `TZ=America/Lima`, `ASPNETCORE_URLS=http://0.0.0.0:8080`,
`ASPNETCORE_ENVIRONMENT=Production`.

## Nginx

Raíz de un dominio/puerto:

```nginx
location / {
    proxy_pass http://127.0.0.1:3010;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}
```

Bajo un prefijo (p. ej. `/urbano/`), quitando el prefijo antes de pasar al contenedor.
La app no necesita cambios: sus rutas son `/api/...` y `/swagger/...`.

Forma simple (válida si las URLs no llevan `%2F` codificado en los parámetros):

```nginx
location /urbano/ {
    proxy_pass http://127.0.0.1:3010/;   # la barra final quita /urbano
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}
```

Forma que conserva la URL original sin decodificar (usar si las fechas viajan como
`01%2F10%2F2026`, porque la forma simple convierte `%2F` en `/` y la ruta deja de coincidir):

```nginx
# en el bloque http (p. ej. /etc/nginx/conf.d/urbano-map.conf)
map $request_uri $urbano_uri {
    ~^/urbano(/.*)$ $1;
}

# en el server
location /urbano/ {
    proxy_pass http://127.0.0.1:3010$urbano_uri;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}
```

## Logs

Van a stdout (`docker logs velsat-mobile-urbano`), con rotación json-file de 10 MB × 3.
La aplicación no escribe archivos en disco, por eso no hay volúmenes de datos.

## Si bin/, obj/ o credenciales ya estaban en git

```bash
git rm -r --cached --ignore-unmatch VelsatMobile/bin VelsatMobile/obj VelsatMobile.Data/bin VelsatMobile.Data/obj VelsatMobile.Model/bin VelsatMobile.Model/obj .vs
git rm --cached --ignore-unmatch VelsatMobile/appsettings.json VelsatMobile/appsettings.Development.json VelsatMobile/firebase-credentials.json .env
git commit -m "Dejar de versionar binarios y credenciales"
```

Las credenciales que hayan estado en el historial de git deben rotarse.
