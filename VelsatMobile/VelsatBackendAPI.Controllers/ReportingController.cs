using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Microsoft.AspNetCore.Mvc;
using VelsatBackendAPI.Model;
using VelsatMobile.Data.Repositories;

namespace VelsatBackendAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReportingController : ControllerBase
{
	private readonly IReadOnlyUnitOfWork _readOnlyUow;

	public ReportingController(IReadOnlyUnitOfWork readOnlyUow)
	{
		_readOnlyUow = readOnlyUow;
	}

	[HttpGet("general/{fechaini}/{fechafin}/{deviceID}/{accountID}")]
	public IActionResult GetDataReporting(string fechaini, string fechafin, string deviceID, string accountID)
	{
		try
		{
			Task<DatosReporting> dataReporting = _readOnlyUow.HistoricosRepository.GetDataReporting(fechaini, fechafin, deviceID, accountID);
			return Ok(dataReporting);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new
			{
				message = "Error al obtener los datos del reporte general",
				error = ex.Message
			});
		}
	}

	[HttpGet("downloadExcelG/{fechaini}/{fechafin}/{deviceID}/{accountID}")]
	public async Task<IActionResult> DownloadExcelG(string fechaini, string fechafin, string deviceID, string accountID)
	{
		try
		{
			DatosReporting datosReporting = await _readOnlyUow.HistoricosRepository.GetDataReporting(fechaini, fechafin, deviceID, accountID);
			string user = _readOnlyUow.HistoricosRepository.UserName(deviceID);
			byte[] fileContents = await ConvertDataExcel(datosReporting.ListaTablas, fechaini, fechafin, deviceID, user);
			string fileDownloadName = "reporte_general_gps_" + deviceID + ".xlsx";
			return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new
			{
				message = "Error al generar el archivo Excel",
				error = ex.Message
			});
		}
	}

	private async Task<byte[]> DownloadImageAsync(string imageUrl)
	{
		using HttpClient httpClient = new HttpClient();
		return await httpClient.GetByteArrayAsync(imageUrl);
	}

	private async Task<byte[]> ConvertDataExcel(List<TablasReporting> datos, string fechaini, string fechafin, string deviceID, string user)
	{
		using XLWorkbook xLWorkbook = new XLWorkbook();
		IXLWorksheet iXLWorksheet = xLWorkbook.Worksheets.Add("DatosGps");
		iXLWorksheet.Cell(12, 2).Value = "ITEM";
		iXLWorksheet.Cell(12, 3).Value = "FECHA";
		iXLWorksheet.Cell(12, 4).Value = "HORA";
		iXLWorksheet.Cell(12, 5).Value = "VELOCIDAD";
		iXLWorksheet.Cell(12, 6).Value = "LATITUD";
		iXLWorksheet.Cell(12, 7).Value = "LONGITUD";
		iXLWorksheet.Cell(12, 8).Value = "ODÓMETRO";
		iXLWorksheet.Cell(12, 9).Value = "UBICACIÓN";
		bool flag = true;
		for (int i = 0; i < datos.Count; i++)
		{
			TablasReporting tablasReporting = datos[i];
		}
		iXLWorksheet.Range("B9:C9").Merge();
		iXLWorksheet.Cell(9, 2).Value = "Fecha de Inicio:";
		iXLWorksheet.Cell(9, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLWorksheet.Cell(9, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell(9, 2).Style.Font.FontColor = XLColor.White;
		iXLWorksheet.Cell(9, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Cell(9, 2).Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell(9, 2).Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell(9, 2).Style.Font.SetBold();
		iXLWorksheet.Range("B10:C10").Merge();
		iXLWorksheet.Cell(10, 2).Value = "Fecha de Fin:";
		iXLWorksheet.Cell(10, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLWorksheet.Cell(10, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell(10, 2).Style.Font.FontColor = XLColor.White;
		iXLWorksheet.Cell(10, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Cell(10, 2).Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell(10, 2).Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell(10, 2).Style.Font.SetBold();
		string text = DateTime.Parse(fechaini).ToString("dd/MM/yyyy  HH:mm");
		iXLWorksheet.Range("D9:E9").Merge();
		iXLWorksheet.Cell(9, 4).Value = text;
		iXLWorksheet.Cell(9, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLWorksheet.Cell(9, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell(9, 4).Style.Font.FontColor = XLColor.White;
		iXLWorksheet.Cell(9, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Cell(9, 4).Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell(9, 4).Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell(9, 4).Style.Font.SetBold();
		string text2 = DateTime.Parse(fechafin).ToString("dd/MM/yyyy  HH:mm");
		iXLWorksheet.Range("D10:E10").Merge();
		iXLWorksheet.Cell(10, 4).Value = text2;
		iXLWorksheet.Cell(10, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLWorksheet.Cell(10, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell(10, 4).Style.Font.FontColor = XLColor.White;
		iXLWorksheet.Cell(10, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Cell(10, 4).Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell(10, 4).Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell(10, 4).Style.Font.SetBold();
		iXLWorksheet.Range("B10:E10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
		iXLWorksheet.Range("B10:E10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Range("F10:I10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
		iXLWorksheet.Range("F10:I10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");
		IXLRange iXLRange = iXLWorksheet.Range("B4:H7");
		iXLRange.Merge();
		iXLRange.Value = "REPORTE GENERAL DE LA UNIDAD: " + deviceID.ToUpper();
		iXLRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLRange.Style.Font.FontColor = XLColor.White;
		iXLRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLRange.Style.Font.FontName = "Calibri";
		iXLRange.Style.Font.FontSize = 16.0;
		iXLRange.Style.Font.SetBold();
		iXLWorksheet.Cell("I9").Value = "Generado el " + DateTime.Now.ToString("dd/MM/yyyy  HH:mm:ss");
		iXLWorksheet.Cell("I9").Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell("I9").Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell("I9").Style.Font.SetBold();
		iXLWorksheet.Cell("I9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
		iXLWorksheet.Cell("I9").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell("I10").Value = "USUARIO : " + user.ToUpper();
		iXLWorksheet.Cell("I10").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
		iXLWorksheet.Cell("I10").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell("I10").Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell("I10").Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell("I10").Style.Font.SetBold();
		byte[] imageBytes1 = await DownloadImageAsync("https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/e880b9a3-e8f9-4278-9d06-6c2f661b8800/public");
		using MemoryStream ms1 = new MemoryStream(imageBytes1);
		IXLPicture iXLPicture = iXLWorksheet.AddPicture(ms1).MoveTo(iXLWorksheet.Cell("B4")).WithSize(81, 81);
		IXLRange iXLRange2 = iXLWorksheet.Range("I4:I7");
		iXLRange2.Merge();
		iXLRange2.Style.Fill.BackgroundColor = XLColor.FromColor(Color.FromArgb(224, 224, 224));
		iXLRange2.Merge().Style.Alignment.WrapText = true;
		iXLRange2.Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLRange2.Merge().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		byte[] imageBytes2 = await DownloadImageAsync("https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/5fb05ad0-957b-4de1-ca5a-3eb24882fa00/public");
		using MemoryStream ms2 = new MemoryStream(imageBytes2);
		IXLPicture iXLPicture2 = iXLWorksheet.AddPicture(ms2).MoveTo(iXLWorksheet.Cell("I4"), new Point(100, 0)).WithSize(240, 80);
		iXLWorksheet.Row(12).Height = 40.0;
		for (int j = 2; j <= 9; j++)
		{
			iXLWorksheet.Cell(12, j).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
			iXLWorksheet.Cell(12, j).Style.Font.Bold = true;
			iXLWorksheet.Cell(12, j).Style.Font.FontColor = XLColor.White;
			iXLWorksheet.Cell(12, j).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			iXLWorksheet.Cell(12, j).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		}
		for (int k = 0; k < datos.Count; k++)
		{
			TablasReporting tablasReporting2 = datos[k];
			string htmlColor = (flag ? "#EDEFF2" : "#FFFFFF");
			XLColor backgroundColor = XLColor.FromHtml(htmlColor);
			for (int l = 2; l <= 9; l++)
			{
				iXLWorksheet.Cell(k + 13, l).Value = l switch
				{
					8 => Math.Round(tablasReporting2.OdometerKM, 2), 
					7 => Math.Round(tablasReporting2.Longitude, 5), 
					6 => Math.Round(tablasReporting2.Latitude, 5), 
					5 => tablasReporting2.SpeedKPH.ToString("0.0") + " KM/H", 
					4 => tablasReporting2.Hora, 
					3 => tablasReporting2.Fecha, 
					2 => k + 1, 
					_ => tablasReporting2.Address, 
				};
				iXLWorksheet.Cell(k + 13, l).Style.Fill.BackgroundColor = backgroundColor;
				iXLWorksheet.Cell(k + 13, l).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				iXLWorksheet.Cell(k + 13, l).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
				if (l == 9)
				{
					iXLWorksheet.Cell(k + 13, l).Style.Alignment.WrapText = true;
				}
			}
			flag = !flag;
		}
		iXLWorksheet.ShowGridLines = false;
		iXLWorksheet.Column(2).Width = 7.0;
		iXLWorksheet.Column(3).Width = 15.0;
		iXLWorksheet.Column(4).Width = 15.0;
		iXLWorksheet.Column(5).Width = 18.0;
		iXLWorksheet.Column(6).Width = 18.0;
		iXLWorksheet.Column(7).Width = 18.0;
		iXLWorksheet.Column(8).Width = 18.0;
		iXLWorksheet.Column(9).Width = 65.0;
		IXLRange iXLRange3 = iXLWorksheet.Range(iXLWorksheet.Cell(12, 2), iXLWorksheet.LastCellUsed(XLCellsUsedOptions.All));
		iXLRange3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLRange3.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		int num = 12 + datos.Count + 1;
		iXLWorksheet.Cell(num, 2).Value = "Datos obtenidos del Sistema de Rastreo Vehicular de www.velsat.com.pe";
		iXLWorksheet.Range(num, 2, num, 8).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
		iXLWorksheet.Range(num, 2, num, 8).Merge().Style.Font.Italic = true;
		iXLWorksheet.Cell(num, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Cell(num, 2).Style.Font.FontColor = XLColor.White;
		using MemoryStream memoryStream = new MemoryStream();
		xLWorkbook.SaveAs(memoryStream);
		return memoryStream.ToArray();
	}

	[HttpGet("speed/{fechaini}/{fechafin}/{deviceId}/{speedKPH}/{accountID}")]
	public IActionResult GetDataSpeed(string fechaini, string fechafin, string deviceId, double speedKPH, string accountID)
	{
		try
		{
			Task<List<SpeedReporting>> speedData = _readOnlyUow.HistoricosRepository.GetSpeedData(fechaini, fechafin, deviceId, speedKPH, accountID);
			return Ok(speedData);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new
			{
				message = "Error al obtener los datos de velocidad",
				error = ex.Message
			});
		}
	}

	[HttpGet("downloadExcelV/{fechaini}/{fechafin}/{deviceId}/{speedKPH}/{accountID}")]
	public async Task<IActionResult> DownloadExcelV(string fechaini, string fechafin, string deviceId, double speedKPH, string accountID)
	{
		try
		{
			List<SpeedReporting> speedData = await _readOnlyUow.HistoricosRepository.GetSpeedData(fechaini, fechafin, deviceId, speedKPH, accountID);
			string user = _readOnlyUow.HistoricosRepository.UserName(deviceId);
			byte[] fileContents = await ConvertSpeedDataExcel(speedData, fechaini, fechafin, deviceId, user);
			string fileDownloadName = "reporte_velocidad_gps_" + deviceId + ".xlsx";
			return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new
			{
				message = "Error al generar el archivo Excel de velocidad",
				error = ex.Message
			});
		}
	}

	private async Task<byte[]> ConvertSpeedDataExcel(List<SpeedReporting> speedData, string fechaini, string fechafin, string deviceId, string user)
	{
		using XLWorkbook xLWorkbook = new XLWorkbook();
		IXLWorksheet iXLWorksheet = xLWorkbook.Worksheets.Add("SpeedData");
		iXLWorksheet.Cell(12, 2).Value = "ITEM";
		iXLWorksheet.Cell(12, 3).Value = "VELOCIDAD";
		iXLWorksheet.Cell(12, 4).Value = "FECHA";
		iXLWorksheet.Cell(12, 5).Value = "HORA";
		iXLWorksheet.Cell(12, 6).Value = "LATITUD";
		iXLWorksheet.Cell(12, 7).Value = "LONGITUD";
		iXLWorksheet.Cell(12, 8).Value = "UBICACIÓN";
		iXLWorksheet.Range("H12:J12").Merge();
		bool flag = true;
		for (int i = 0; i < speedData.Count; i++)
		{
			SpeedReporting speedReporting = speedData[i];
			iXLWorksheet.Cell(i + 13, 2).Value = speedReporting.Item;
			iXLWorksheet.Cell(i + 13, 3).Value = speedReporting.SpeedKPH;
			iXLWorksheet.Cell(i + 13, 4).Value = speedReporting.Date;
			iXLWorksheet.Cell(i + 13, 5).Value = speedReporting.Time;
			iXLWorksheet.Cell(i + 13, 6).Value = Math.Round(speedReporting.Latitude, 5);
			iXLWorksheet.Cell(i + 13, 7).Value = Math.Round(speedReporting.Longitude, 5);
			iXLWorksheet.Cell(i + 13, 8).Value = speedReporting.Address;
			iXLWorksheet.Range(i + 13, 8, i + 13, 10).Merge();
		}
		iXLWorksheet.Range("B9:C9").Merge();
		iXLWorksheet.Cell(9, 2).Value = "Fecha de Inicio:";
		iXLWorksheet.Cell(9, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLWorksheet.Cell(9, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell(9, 2).Style.Font.FontColor = XLColor.White;
		iXLWorksheet.Cell(9, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Cell(9, 2).Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell(9, 2).Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell(9, 2).Style.Font.SetBold();
		iXLWorksheet.Range("B10:C10").Merge();
		iXLWorksheet.Cell(10, 2).Value = "Fecha de Fin:";
		iXLWorksheet.Cell(10, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLWorksheet.Cell(10, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell(10, 2).Style.Font.FontColor = XLColor.White;
		iXLWorksheet.Cell(10, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Cell(10, 2).Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell(10, 2).Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell(10, 2).Style.Font.SetBold();
		iXLWorksheet.Range("B10:E10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
		iXLWorksheet.Range("B10:E10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");
		string text = DateTime.Parse(fechaini).ToString("dd/MM/yyyy  HH:mm");
		iXLWorksheet.Range("D9:E9").Merge();
		iXLWorksheet.Cell(9, 4).Value = text;
		iXLWorksheet.Cell(9, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLWorksheet.Cell(9, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell(9, 4).Style.Font.FontColor = XLColor.White;
		iXLWorksheet.Cell(9, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Cell(9, 4).Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell(9, 4).Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell(9, 4).Style.Font.SetBold();
		string text2 = DateTime.Parse(fechafin).ToString("dd/MM/yyyy  HH:mm");
		iXLWorksheet.Range("D10:E10").Merge();
		iXLWorksheet.Cell(10, 4).Value = text2;
		iXLWorksheet.Cell(10, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLWorksheet.Cell(10, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell(10, 4).Style.Font.FontColor = XLColor.White;
		iXLWorksheet.Cell(10, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Cell(10, 4).Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell(10, 4).Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell(10, 4).Style.Font.SetBold();
		iXLWorksheet.Range("F10:J10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
		iXLWorksheet.Range("F10:J10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");
		IXLRange iXLRange = iXLWorksheet.Range("B4:H7");
		iXLRange.Merge();
		iXLRange.Value = "REPORTE DE VELOCIDAD DE LA UNIDAD: " + deviceId.ToUpper();
		iXLRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLRange.Style.Font.FontColor = XLColor.White;
		iXLRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLRange.Style.Font.FontName = "Calibri";
		iXLRange.Style.Font.FontSize = 16.0;
		iXLRange.Style.Font.SetBold();
		iXLWorksheet.Cell("J9").Value = "Generado el " + DateTime.Now.ToString("dd/MM/yyyy  HH:mm:ss");
		iXLWorksheet.Cell("J9").Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell("J9").Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell("J9").Style.Font.SetBold();
		iXLWorksheet.Cell("J9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
		iXLWorksheet.Cell("J9").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell("J10").Value = "USUARIO : " + user.ToUpper();
		iXLWorksheet.Cell("J10").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
		iXLWorksheet.Cell("J10").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell("J10").Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell("J10").Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell("J10").Style.Font.SetBold();
		byte[] imageBytes1 = await DownloadImageAsync("https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/e880b9a3-e8f9-4278-9d06-6c2f661b8800/public");
		using MemoryStream ms1 = new MemoryStream(imageBytes1);
		IXLPicture iXLPicture = iXLWorksheet.AddPicture(ms1).MoveTo(iXLWorksheet.Cell("B4")).WithSize(81, 81);
		IXLRange iXLRange2 = iXLWorksheet.Range("I4:J7");
		iXLRange2.Merge();
		iXLRange2.Style.Fill.BackgroundColor = XLColor.FromColor(Color.FromArgb(224, 224, 224));
		iXLRange2.Merge().Style.Alignment.WrapText = true;
		iXLRange2.Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLRange2.Merge().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		byte[] imageBytes2 = await DownloadImageAsync("https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/5fb05ad0-957b-4de1-ca5a-3eb24882fa00/public");
		using MemoryStream ms2 = new MemoryStream(imageBytes2);
		IXLPicture iXLPicture2 = iXLWorksheet.AddPicture(ms2).MoveTo(iXLWorksheet.Cell("I4")).WithSize(240, 80)
			.MoveTo(815, 60);
		iXLWorksheet.Row(12).Height = 40.0;
		for (int j = 2; j <= 10; j++)
		{
			iXLWorksheet.Cell(12, j).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
			iXLWorksheet.Cell(12, j).Style.Font.Bold = true;
			iXLWorksheet.Cell(12, j).Style.Font.FontColor = XLColor.White;
			iXLWorksheet.Cell(12, j).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			iXLWorksheet.Cell(12, j).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		}
		for (int k = 0; k < speedData.Count; k++)
		{
			SpeedReporting speedReporting2 = speedData[k];
			string htmlColor = (flag ? "#EDEFF2" : "#FFFFFF");
			XLColor backgroundColor = XLColor.FromHtml(htmlColor);
			for (int l = 2; l <= 10; l++)
			{
				iXLWorksheet.Cell(k + 13, l).Style.Fill.BackgroundColor = backgroundColor;
				iXLWorksheet.Cell(k + 13, l).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				iXLWorksheet.Cell(k + 13, l).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
				if (l == 8)
				{
					iXLWorksheet.Cell(k + 13, l).Style.Alignment.WrapText = true;
				}
			}
			flag = !flag;
			flag = !flag;
		}
		iXLWorksheet.ShowGridLines = false;
		iXLWorksheet.Column(2).Width = 7.0;
		iXLWorksheet.Column(3).Width = 15.0;
		iXLWorksheet.Column(4).Width = 15.0;
		iXLWorksheet.Column(5).Width = 15.0;
		iXLWorksheet.Column(6).Width = 15.0;
		iXLWorksheet.Column(7).Width = 18.0;
		iXLWorksheet.Column(8).Width = 17.0;
		iXLWorksheet.Column(9).Width = 26.0;
		iXLWorksheet.Column(10).Width = 7.0;
		IXLRange iXLRange3 = iXLWorksheet.Range(iXLWorksheet.Cell(12, 2), iXLWorksheet.LastCellUsed(XLCellsUsedOptions.All));
		iXLRange3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLRange3.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		int num = 13 + speedData.Count;
		iXLWorksheet.Cell(num, 2).Value = "Datos obtenidos del Sistema de Rastreo Vehicular de www.velsat.com.pe";
		iXLWorksheet.Range(num, 2, num, 10).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
		iXLWorksheet.Range(num, 2, num, 10).Merge().Style.Font.Italic = true;
		iXLWorksheet.Cell(num, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Cell(num, 2).Style.Font.FontColor = XLColor.White;
		using MemoryStream memoryStream = new MemoryStream();
		xLWorkbook.SaveAs(memoryStream);
		return memoryStream.ToArray();
	}

	[HttpGet("stops/{fechaini}/{fechafin}/{deviceId}/{accountID}")]
	public IActionResult GetDataStops(string fechaini, string fechafin, string deviceId, string accountID)
	{
		try
		{
			Task<List<StopsReporting>> stopData = _readOnlyUow.HistoricosRepository.GetStopData(fechaini, fechafin, deviceId, accountID);
			return Ok(stopData);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new
			{
				message = "Error al obtener los datos de paradas",
				error = ex.Message
			});
		}
	}

	[HttpGet("downloadExcelS/{fechaini}/{fechafin}/{deviceID}/{accountID}")]
	public async Task<IActionResult> DownloadExcelS(string fechaini, string fechafin, string deviceID, string accountID)
	{
		try
		{
			List<StopsReporting> stopsData = await _readOnlyUow.HistoricosRepository.GetStopData(fechaini, fechafin, deviceID, accountID);
			string user = _readOnlyUow.HistoricosRepository.UserName(deviceID);
			byte[] fileContents = await ConvertStopDataExcel(stopsData, fechaini, fechafin, deviceID, user);
			string fileDownloadName = "reporte_paradas_gps_" + deviceID + ".xlsx";
			return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new
			{
				message = "Error al generar el archivo Excel de paradas",
				error = ex.Message
			});
		}
	}

	private async Task<byte[]> ConvertStopDataExcel(List<StopsReporting> stopsData, string fechaini, string fechafin, string deviceId, string user)
	{
		using XLWorkbook xLWorkbook = new XLWorkbook();
		IXLWorksheet iXLWorksheet = xLWorkbook.Worksheets.Add("StopsData");
		iXLWorksheet.Cell(12, 2).Value = "ITEM";
		iXLWorksheet.Cell(12, 3).Value = "FECHA INICIO";
		iXLWorksheet.Cell(12, 4).Value = "HORA INICIO";
		iXLWorksheet.Cell(12, 5).Value = "FECHA FINAL";
		iXLWorksheet.Cell(12, 6).Value = "HORA FINAL";
		iXLWorksheet.Cell(12, 7).Value = "TIEMPO TOTAL";
		iXLWorksheet.Cell(12, 8).Value = "LATITUD";
		iXLWorksheet.Cell(12, 9).Value = "LONGITUD";
		iXLWorksheet.Cell(12, 10).Value = "UBICACIÓN";
		bool flag = true;
		for (int i = 0; i < stopsData.Count; i++)
		{
			StopsReporting stopsReporting = stopsData[i];
			iXLWorksheet.Cell(i + 13, 2).Value = stopsReporting.Item;
			iXLWorksheet.Cell(i + 13, 3).Value = stopsReporting.StartDate;
			iXLWorksheet.Cell(i + 13, 4).Value = stopsReporting.StartTime;
			iXLWorksheet.Cell(i + 13, 5).Value = stopsReporting.EndDate;
			iXLWorksheet.Cell(i + 13, 6).Value = stopsReporting.EndTime;
			iXLWorksheet.Cell(i + 13, 7).Value = stopsReporting.TotalTime;
			iXLWorksheet.Cell(i + 13, 8).Value = Math.Round(stopsReporting.Latitude, 5);
			iXLWorksheet.Cell(i + 13, 9).Value = Math.Round(stopsReporting.Longitude, 5);
			iXLWorksheet.Cell(i + 13, 10).Value = stopsReporting.Address;
		}
		iXLWorksheet.Range("B9:C9").Merge();
		iXLWorksheet.Cell(9, 2).Value = "Fecha de Inicio:";
		iXLWorksheet.Cell(9, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLWorksheet.Cell(9, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell(9, 2).Style.Font.FontColor = XLColor.White;
		iXLWorksheet.Cell(9, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Cell(9, 2).Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell(9, 2).Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell(9, 2).Style.Font.SetBold();
		iXLWorksheet.Range("B10:C10").Merge();
		iXLWorksheet.Cell(10, 2).Value = "Fecha de Fin:";
		iXLWorksheet.Cell(10, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLWorksheet.Cell(10, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell(10, 2).Style.Font.FontColor = XLColor.White;
		iXLWorksheet.Cell(10, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Cell(10, 2).Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell(10, 2).Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell(10, 2).Style.Font.SetBold();
		iXLWorksheet.Range("B10:E10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
		iXLWorksheet.Range("B10:E10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");
		string text = DateTime.Parse(fechaini).ToString("dd/MM/yyyy  HH:mm");
		iXLWorksheet.Range("D9:E9").Merge();
		iXLWorksheet.Cell(9, 4).Value = text;
		iXLWorksheet.Cell(9, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLWorksheet.Cell(9, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell(9, 4).Style.Font.FontColor = XLColor.White;
		iXLWorksheet.Cell(9, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Cell(9, 4).Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell(9, 4).Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell(9, 4).Style.Font.SetBold();
		string text2 = DateTime.Parse(fechafin).ToString("dd/MM/yyyy  HH:mm");
		iXLWorksheet.Range("D10:E10").Merge();
		iXLWorksheet.Cell(10, 4).Value = text2;
		iXLWorksheet.Cell(10, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLWorksheet.Cell(10, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell(10, 4).Style.Font.FontColor = XLColor.White;
		iXLWorksheet.Cell(10, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Cell(10, 4).Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell(10, 4).Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell(10, 4).Style.Font.SetBold();
		iXLWorksheet.Range("F10:J10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
		iXLWorksheet.Range("F10:J10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");
		IXLRange iXLRange = iXLWorksheet.Range("B4:H7");
		iXLRange.Merge();
		iXLRange.Value = "REPORTE DE PARADAS DE LA UNIDAD: " + deviceId.ToUpper();
		iXLRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLRange.Style.Font.FontColor = XLColor.White;
		iXLRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLRange.Style.Font.FontName = "Calibri";
		iXLRange.Style.Font.FontSize = 16.0;
		iXLRange.Style.Font.SetBold();
		iXLWorksheet.Cell("J9").Value = "Generado el " + DateTime.Now.ToString("dd/MM/yyyy  HH:mm:ss");
		iXLWorksheet.Cell("J9").Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell("J9").Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell("J9").Style.Font.SetBold();
		iXLWorksheet.Cell("J9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
		iXLWorksheet.Cell("J9").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell("J10").Value = "USUARIO : " + user.ToUpper();
		iXLWorksheet.Cell("J10").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
		iXLWorksheet.Cell("J10").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLWorksheet.Cell("J10").Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell("J10").Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell("J10").Style.Font.SetBold();
		byte[] imageBytes1 = await DownloadImageAsync("https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/e880b9a3-e8f9-4278-9d06-6c2f661b8800/public");
		using MemoryStream ms1 = new MemoryStream(imageBytes1);
		IXLPicture iXLPicture = iXLWorksheet.AddPicture(ms1).MoveTo(iXLWorksheet.Cell("B4")).WithSize(81, 81);
		IXLRange iXLRange2 = iXLWorksheet.Range("I4:J7");
		iXLRange2.Merge();
		iXLRange2.Style.Fill.BackgroundColor = XLColor.FromColor(Color.FromArgb(224, 224, 224));
		iXLRange2.Merge().Style.Alignment.WrapText = true;
		iXLRange2.Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLRange2.Merge().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		byte[] imageBytes2 = await DownloadImageAsync("https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/5fb05ad0-957b-4de1-ca5a-3eb24882fa00/public");
		using MemoryStream ms2 = new MemoryStream(imageBytes2);
		IXLPicture iXLPicture2 = iXLWorksheet.AddPicture(ms2).MoveTo(iXLWorksheet.Cell("I4")).WithSize(240, 80)
			.MoveTo(960, 60);
		iXLWorksheet.Row(12).Height = 40.0;
		for (int j = 2; j <= 10; j++)
		{
			iXLWorksheet.Cell(12, j).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
			iXLWorksheet.Cell(12, j).Style.Font.Bold = true;
			iXLWorksheet.Cell(12, j).Style.Font.FontColor = XLColor.White;
			iXLWorksheet.Cell(12, j).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			iXLWorksheet.Cell(12, j).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		}
		for (int k = 0; k < stopsData.Count; k++)
		{
			StopsReporting stopsReporting2 = stopsData[k];
			string htmlColor = (flag ? "#EDEFF2" : "#FFFFFF");
			XLColor backgroundColor = XLColor.FromHtml(htmlColor);
			for (int l = 2; l <= 10; l++)
			{
				iXLWorksheet.Cell(k + 13, l).Style.Fill.BackgroundColor = backgroundColor;
				iXLWorksheet.Cell(k + 13, l).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				iXLWorksheet.Cell(k + 13, l).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
				if (l == 8)
				{
					iXLWorksheet.Cell(k + 13, l).Style.Alignment.WrapText = true;
				}
			}
			flag = !flag;
			flag = !flag;
		}
		iXLWorksheet.ShowGridLines = false;
		iXLWorksheet.Column(2).Width = 7.0;
		iXLWorksheet.Column(3).Width = 15.0;
		iXLWorksheet.Column(4).Width = 15.0;
		iXLWorksheet.Column(5).Width = 15.0;
		iXLWorksheet.Column(6).Width = 15.0;
		iXLWorksheet.Column(7).Width = 18.0;
		iXLWorksheet.Column(8).Width = 18.0;
		iXLWorksheet.Column(9).Width = 18.0;
		iXLWorksheet.Column(10).Width = 55.0;
		IXLRange iXLRange3 = iXLWorksheet.Range(iXLWorksheet.Cell(12, 2), iXLWorksheet.LastCellUsed(XLCellsUsedOptions.All));
		iXLRange3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLRange3.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		int num = 13 + stopsData.Count;
		iXLWorksheet.Cell(num, 2).Value = "Datos obtenidos del Sistema de Rastreo Vehicular de www.velsat.com.pe";
		iXLWorksheet.Range(num, 2, num, 10).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
		iXLWorksheet.Range(num, 2, num, 10).Merge().Style.Font.Italic = true;
		iXLWorksheet.Cell(num, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Cell(num, 2).Style.Font.FontColor = XLColor.White;
		using MemoryStream memoryStream = new MemoryStream();
		xLWorkbook.SaveAs(memoryStream);
		return memoryStream.ToArray();
	}

	[HttpGet("details/{fechaini}/{fechafin}/{deviceId}/{accountID}")]
	public IActionResult GetRouteDetails(string fechaini, string fechafin, string deviceId, string accountID)
	{
		try
		{
			Task<List<RouteDetails>> routeDetails = _readOnlyUow.HistoricosRepository.GetRouteDetails(fechaini, fechafin, deviceId, accountID);
			return Ok(routeDetails);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new
			{
				message = "Error al obtener los detalles de ruta",
				error = ex.Message
			});
		}
	}

	[HttpGet("filtersedapal")]
	public async Task<IActionResult> GetDevicesByRuta([FromQuery] string rutadefault)
	{
		if (string.IsNullOrEmpty(rutadefault))
		{
			return BadRequest("El parámetro 'rutadefault' es requerido.");
		}
		try
		{
			List<string> list = await _readOnlyUow.HistoricosRepository.DeviceFilterSedapal(rutadefault);
			if (list == null || !list.Any())
			{
				return NotFound("No se encontraron dispositivos para la ruta especificada.");
			}
			return Ok(list);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new
			{
				message = "Error al obtener los dispositivos filtrados",
				error = ex.Message
			});
		}
	}
}
