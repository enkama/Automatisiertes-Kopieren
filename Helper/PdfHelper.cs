﻿using System;
using System.IO;
using System.Threading.Tasks;
using iText.Forms;
using iText.Kernel.Pdf;
using static Automatisiertes_Kopieren.Helper.LoggingHelper;

namespace Automatisiertes_Kopieren.Helper;

public static class PdfHelper
{
    public enum PdfType
    {
        Protokollbogen,
        AllgemeinEntwicklungsbericht,
        ProtokollElterngespraech,
        VorschuleEntwicklungsbericht
    }

    public static async Task FillPdfAsync(string pdfPath, string kidName, double monthsValue, string group,
        PdfType pdfType,
        string parsedBirthDate, string? genderValue)
    {
        try
        {
            using var pdfDoc = new PdfDocument(new PdfReader(pdfPath), new PdfWriter(pdfPath + ".temp"));

            var form = PdfAcroForm.GetAcroForm(pdfDoc, true) ??
                       throw new Exception("The PDF does not contain any form fields.");
            switch (pdfType)
            {
                case PdfType.Protokollbogen:
                    form.GetField("Name_des_Kindes").SetValue(kidName);
                    form.GetField("Alter_des_Kindes_in_Monaten").SetValue(monthsValue.ToString("0.00"));
                    form.GetField("Gruppe").SetValue(group);
                    form.GetField("Heutiges_Datum").SetValue(DateTime.Now.ToString("dd.MM.yyyy"));
                    form.GetField("Geburtsdatum").SetValue(parsedBirthDate);

                    switch (genderValue)
                    {
                        case "Männlich":
                            form.GetField("männlich").SetValue("On");
                            form.GetField("weiblich").SetValue("Off");
                            break;
                        case "Weiblich":
                            form.GetField("weiblich").SetValue("On");
                            form.GetField("männlich").SetValue("Off");
                            break;
                    }

                    break;

                case PdfType.AllgemeinEntwicklungsbericht:
                    form.GetField("Name").SetValue(kidName);
                    form.GetField("Alter in Monaten").SetValue(monthsValue.ToString("0.00"));
                    form.GetField("Gruppe").SetValue(group);
                    form.GetField("Datum").SetValue(DateTime.Now.ToString("dd.MM.yyyy"));
                    break;

                case PdfType.ProtokollElterngespraech:
                    form.GetField("Name des Kindes").SetValue(kidName);
                    form.GetField("Geburtsdatum").SetValue(parsedBirthDate);
                    break;
                case PdfType.VorschuleEntwicklungsbericht:
                    form.GetField("Name des Kindes").SetValue(kidName);
                    form.GetField("Datum").SetValue(DateTime.Now.ToString("dd.MM.yyyy"));
                    form.GetField("Gruppe").SetValue(group);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pdfType), pdfType, null);
            }

            pdfDoc.Close();
        }
        catch (Exception ex)
        {
            LogMessage(
                $"Error encountered in FillPdf. Message: {ex.Message}. StackTrace: {ex.StackTrace}", LogLevel.Error);
        }

        try
        {
            await Task.Run(() =>
            {
                File.Delete(pdfPath);
                File.Move(pdfPath + ".temp", pdfPath);
            });
        }
        catch (Exception ex)
        {
            LogMessage(
                $"Error encountered while handling file operations. Message: {ex.Message}. StackTrace: {ex.StackTrace}",
                LogLevel.Error);
        }
    }
}