using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
public class KilometerController : ControllerBase
{
	private readonly IReadOnlyUnitOfWork _readOnlyUow;

	public KilometerController(IReadOnlyUnitOfWork readOnlyUow)
	{
		_readOnlyUow = readOnlyUow;
	}

	[HttpGet("kilometer/{fechaini}/{fechafin}/{deviceID}/{accountID}")]
	public async Task<IActionResult> GetReportingKilometer(string fechaini, string fechafin, string deviceID, string accountID)
	{
		return Ok(await _readOnlyUow.KilometrosRepository.GetKmReporting(fechaini, fechafin, deviceID, accountID));
	}

	[HttpGet("downloadExcelK/{fechaini}/{fechafin}/{deviceID}/{accountID}")]
	public async Task<IActionResult> DownloadExcelK(string fechaini, string fechafin, string deviceID, string accountID)
	{
		byte[] fileContents = await ConvertKilometerDataExcel((await _readOnlyUow.KilometrosRepository.GetKmReporting(fechaini, fechafin, deviceID, accountID)).ListaKilometros, fechaini, fechafin, deviceID);
		string fileDownloadName = "reporte_kilometros_gps.xlsx";
		return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName);
	}

	[HttpGet("kilometerall/{fechaini}/{fechafin}/{accountID}")]
	public async Task<IActionResult> GetAllKilometer(string fechaini, string fechafin, string accountID)
	{
		return Ok(await _readOnlyUow.KilometrosRepository.GetAllKmReporting(fechaini, fechafin, accountID));
	}

	[HttpGet("downloadExcelKall/{fechaini}/{fechafin}/{accountID}")]
	public async Task<IActionResult> DownloadExcelKall(string fechaini, string fechafin, string accountID)
	{
		byte[] fileContents = await ConvertKilometerAllExcel((await _readOnlyUow.KilometrosRepository.GetAllKmReporting(fechaini, fechafin, accountID)).ListaKilometros, fechaini, fechafin, accountID);
		string fileDownloadName = "reporte_kilometros_gps.xlsx";
		return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName);
	}

	private async Task<byte[]> DownloadImageAsync(string imageUrl)
	{
		using HttpClient httpClient = new HttpClient();
		return await httpClient.GetByteArrayAsync(imageUrl);
	}

	private async Task<byte[]> ConvertKilometerDataExcel(List<KilometrosRecorridos> datos, string fechaini, string fechafin, string deviceID)
	{
		using XLWorkbook xLWorkbook = new XLWorkbook();
		IXLWorksheet iXLWorksheet = xLWorkbook.Worksheets.Add("DatosGps");
		iXLWorksheet.Cell(12, 2).Value = "ITEM";
		iXLWorksheet.Cell(12, 3).Value = "UNIDAD";
		iXLWorksheet.Cell(12, 4).Value = "KILOMETROS";
		bool flag = true;
		for (int i = 0; i < datos.Count; i++)
		{
			KilometrosRecorridos kilometrosRecorridos = datos[i];
			string htmlColor = (flag ? "#EDEFF2" : "#FFFFFF");
			XLColor backgroundColor = XLColor.FromHtml(htmlColor);
			iXLWorksheet.Cell(i + 13, 2).Value = i + 1;
			iXLWorksheet.Cell(i + 13, 3).Value = kilometrosRecorridos.DeviceId;
			iXLWorksheet.Cell(i + 13, 4).Value = Math.Round(kilometrosRecorridos.Kilometros, 2) + " Km";
			for (int j = 2; j <= 4; j++)
			{
				iXLWorksheet.Cell(i + 13, j).Style.Fill.BackgroundColor = backgroundColor;
				iXLWorksheet.Cell(i + 13, j).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				iXLWorksheet.Cell(i + 13, j).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
			}
			flag = !flag;
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
		iXLWorksheet.Range("F10:H10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
		iXLWorksheet.Range("F10:H10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");
		IXLRange iXLRange = iXLWorksheet.Range("B4:G7");
		iXLRange.Merge();
		iXLRange.Value = "REPORTE DE KILÓMETROS RECORRIDOS";
		iXLRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLRange.Style.Font.FontColor = XLColor.White;
		iXLRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLRange.Style.Font.FontName = "Calibri";
		iXLRange.Style.Font.FontSize = 16.0;
		iXLRange.Style.Font.SetBold();
		iXLWorksheet.Cell("H9").Value = "Generado el " + DateTime.Now.ToString("dd/MM/yyyy  HH:mm:ss");
		iXLWorksheet.Cell("H9").Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell("H9").Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell("H9").Style.Font.SetBold();
		iXLWorksheet.Cell("H9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
		iXLWorksheet.Cell("H9").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		byte[] imageBytes1 = await DownloadImageAsync("https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/e880b9a3-e8f9-4278-9d06-6c2f661b8800/public");
		using MemoryStream ms1 = new MemoryStream(imageBytes1);
		IXLPicture iXLPicture = iXLWorksheet.AddPicture(ms1).MoveTo(iXLWorksheet.Cell("B4")).WithSize(81, 81);
		IXLRange iXLRange2 = iXLWorksheet.Range("H4:H7");
		iXLRange2.Merge();
		iXLRange2.Style.Fill.BackgroundColor = XLColor.FromColor(Color.FromArgb(224, 224, 224));
		iXLRange2.Merge().Style.Alignment.WrapText = true;
		iXLRange2.Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLRange2.Merge().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		byte[] imageBytes2 = await DownloadImageAsync("https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/5fb05ad0-957b-4de1-ca5a-3eb24882fa00/public");
		using MemoryStream ms2 = new MemoryStream(imageBytes2);
		IXLPicture iXLPicture2 = iXLWorksheet.AddPicture(ms2).MoveTo(iXLWorksheet.Cell("H4")).WithSize(240, 80)
			.MoveTo(800, 60);
		iXLWorksheet.Row(12).Height = 40.0;
		for (int k = 2; k <= 4; k++)
		{
			iXLWorksheet.Cell(12, k).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
			iXLWorksheet.Cell(12, k).Style.Font.Bold = true;
			iXLWorksheet.Cell(12, k).Style.Font.FontColor = XLColor.White;
			iXLWorksheet.Cell(12, k).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			iXLWorksheet.Cell(12, k).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		}
		iXLWorksheet.ShowGridLines = false;
		iXLWorksheet.Column(2).Width = 7.0;
		iXLWorksheet.Column(3).Width = 15.0;
		iXLWorksheet.Column(4).Width = 15.0;
		iXLWorksheet.Column(5).Width = 18.0;
		iXLWorksheet.Column(6).Width = 18.0;
		iXLWorksheet.Column(7).Width = 18.0;
		iXLWorksheet.Column(8).Width = 55.0;
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

	private async Task<byte[]> ConvertKilometerAllExcel(List<KilometrosRecorridos> listaKilometros, string fechaini, string fechafin, string accountID)
	{
		using XLWorkbook xLWorkbook = new XLWorkbook();
		IXLWorksheet iXLWorksheet = xLWorkbook.Worksheets.Add("DatosGps");
		iXLWorksheet.Cell(12, 2).Value = "ITEM";
		iXLWorksheet.Cell(12, 3).Value = "UNIDAD";
		iXLWorksheet.Cell(12, 4).Value = "KILOMETROS";
		bool flag = true;
		for (int i = 0; i < listaKilometros.Count; i++)
		{
			KilometrosRecorridos kilometrosRecorridos = listaKilometros[i];
			string htmlColor = (flag ? "#EDEFF2" : "#FFFFFF");
			XLColor backgroundColor = XLColor.FromHtml(htmlColor);
			iXLWorksheet.Cell(i + 13, 2).Value = i + 1;
			iXLWorksheet.Cell(i + 13, 3).Value = kilometrosRecorridos.DeviceId;
			iXLWorksheet.Cell(i + 13, 4).Value = Math.Round(kilometrosRecorridos.Maximo - kilometrosRecorridos.Minimo, 2) + " Km";
			for (int j = 2; j <= 4; j++)
			{
				iXLWorksheet.Cell(i + 13, j).Style.Fill.BackgroundColor = backgroundColor;
				iXLWorksheet.Cell(i + 13, j).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				iXLWorksheet.Cell(i + 13, j).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
			}
			flag = !flag;
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
		iXLWorksheet.Range("F10:H10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
		iXLWorksheet.Range("F10:H10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");
		IXLRange iXLRange = iXLWorksheet.Range("B4:G7");
		iXLRange.Merge();
		iXLRange.Value = "REPORTE DE KILÓMETROS RECORRIDOS";
		iXLRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		iXLRange.Style.Font.FontColor = XLColor.White;
		iXLRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLRange.Style.Font.FontName = "Calibri";
		iXLRange.Style.Font.FontSize = 16.0;
		iXLRange.Style.Font.SetBold();
		iXLWorksheet.Cell("H9").Value = "Generado el " + DateTime.Now.ToString("dd/MM/yyyy  HH:mm:ss");
		iXLWorksheet.Cell("H9").Style.Font.FontName = "Cambria";
		iXLWorksheet.Cell("H9").Style.Font.FontSize = 10.0;
		iXLWorksheet.Cell("H9").Style.Font.SetBold();
		iXLWorksheet.Cell("H9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
		iXLWorksheet.Cell("H9").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		byte[] imageBytes1 = await DownloadImageAsync("https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/e880b9a3-e8f9-4278-9d06-6c2f661b8800/public");
		using MemoryStream ms1 = new MemoryStream(imageBytes1);
		IXLPicture iXLPicture = iXLWorksheet.AddPicture(ms1).MoveTo(iXLWorksheet.Cell("B4")).WithSize(81, 81);
		IXLRange iXLRange2 = iXLWorksheet.Range("H4:H7");
		iXLRange2.Merge();
		iXLRange2.Style.Fill.BackgroundColor = XLColor.FromColor(Color.FromArgb(224, 224, 224));
		iXLRange2.Merge().Style.Alignment.WrapText = true;
		iXLRange2.Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLRange2.Merge().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		byte[] imageBytes2 = await DownloadImageAsync("https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/5fb05ad0-957b-4de1-ca5a-3eb24882fa00/public");
		using MemoryStream ms2 = new MemoryStream(imageBytes2);
		IXLPicture iXLPicture2 = iXLWorksheet.AddPicture(ms2).MoveTo(iXLWorksheet.Cell("H4")).WithSize(240, 80)
			.MoveTo(800, 60);
		iXLWorksheet.Row(12).Height = 40.0;
		for (int k = 2; k <= 4; k++)
		{
			iXLWorksheet.Cell(12, k).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
			iXLWorksheet.Cell(12, k).Style.Font.Bold = true;
			iXLWorksheet.Cell(12, k).Style.Font.FontColor = XLColor.White;
			iXLWorksheet.Cell(12, k).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			iXLWorksheet.Cell(12, k).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		}
		iXLWorksheet.ShowGridLines = false;
		iXLWorksheet.Column(2).Width = 7.0;
		iXLWorksheet.Column(3).Width = 15.0;
		iXLWorksheet.Column(4).Width = 15.0;
		iXLWorksheet.Column(5).Width = 18.0;
		iXLWorksheet.Column(6).Width = 18.0;
		iXLWorksheet.Column(7).Width = 18.0;
		iXLWorksheet.Column(8).Width = 55.0;
		IXLRange iXLRange3 = iXLWorksheet.Range(iXLWorksheet.Cell(12, 2), iXLWorksheet.LastCellUsed(XLCellsUsedOptions.All));
		iXLRange3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
		iXLRange3.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		int num = 12 + listaKilometros.Count + 1;
		iXLWorksheet.Cell(num, 2).Value = "Datos obtenidos del Sistema de Rastreo Vehicular de www.velsat.com.pe";
		iXLWorksheet.Range(num, 2, num, 8).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
		iXLWorksheet.Range(num, 2, num, 8).Merge().Style.Font.Italic = true;
		iXLWorksheet.Cell(num, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
		iXLWorksheet.Cell(num, 2).Style.Font.FontColor = XLColor.White;
		using MemoryStream memoryStream = new MemoryStream();
		xLWorkbook.SaveAs(memoryStream);
		return memoryStream.ToArray();
	}
}
