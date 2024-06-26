﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Automatisiertes_Kopieren.Helper.FileManagerHelper.StringUtilities;
using static Automatisiertes_Kopieren.Helper.LoggingHelper;

namespace Automatisiertes_Kopieren.Helper
{
    public static class ValidationHelper
    {
        public static (string directoryPath, string fileName)? FindProtokollbogenForMonths(double monthsAndDays)
        {
            var protokollbogenRanges = new List<(double start, double end, (string directoryPath, string fileName) value)>
            {
                (10.15, 16.14,
                    (Path.Combine("Entwicklungsboegen", "Krippe-Protokollboegen"), "Kind_Protokollbogen_12_Monate")),
                (16.15, 22.14,
                    (Path.Combine("Entwicklungsboegen", "Krippe-Protokollboegen"), "Kind_Protokollbogen_18_Monate")),
                (22.15, 27.14,
                    (Path.Combine("Entwicklungsboegen", "Krippe-Protokollboegen"), "Kind_Protokollbogen_24_Monate")),
                (27.15, 33.14,
                    (Path.Combine("Entwicklungsboegen", "Krippe-Protokollboegen"), "Kind_Protokollbogen_30_Monate")),
                (33.15, 39.14,
                    (Path.Combine("Entwicklungsboegen", "Krippe-Protokollboegen"), "Kind_Protokollbogen_36_Monate")),
                (39.15, 45.14,
                    (Path.Combine("Entwicklungsboegen", "Ele-Protokollboegen"), "Kind_Protokollbogen_42_Monate")),
                (45.15, 51.14,
                    (Path.Combine("Entwicklungsboegen", "Ele-Protokollboegen"), "Kind_Protokollbogen_48_Monate")),
                (51.15, 57.14,
                    (Path.Combine("Entwicklungsboegen", "Ele-Protokollboegen"), "Kind_Protokollbogen_54_Monate")),
                (57.15, 63.14,
                    (Path.Combine("Entwicklungsboegen", "Ele-Protokollboegen"), "Kind_Protokollbogen_60_Monate")),
                (63.15, 69.14,
                    (Path.Combine("Entwicklungsboegen", "Ele-Protokollboegen"), "Kind_Protokollbogen_66_Monate")),
                (69.15, 84.00, (Path.Combine("Entwicklungsboegen", "Ele-Protokollboegen"), "Kind_Protokollbogen_72_Monate"))
            };

            foreach (var (start, end, value) in protokollbogenRanges.OrderByDescending(r => r.start))
                if (monthsAndDays >= start && monthsAndDays <= end)
                    return value;

            LogAndShowMessage($"Kein Protokollbogen für Monatswert gefunden: {monthsAndDays}",
                $"Kein Protokollbogen für folgenden Monatswert gefunden: {monthsAndDays}",
                LogLevel.Warning);
            MainWindow.OperationState.OperationsSuccessful = false;
            return null;
        }

        public static double ConvertToDecimalFormat(double monthsAndDays)
        {
            var monthsAndDaysRaw = monthsAndDays.ToString("0.00", CultureInfo.InvariantCulture);
            return double.Parse(monthsAndDaysRaw.Replace(",", "."), CultureInfo.InvariantCulture);
        }

        public static async Task<string?> ValidateKidNameAsync(string kidName, string homeFolder, string groupDropdownText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(kidName))
                {
                    LogAndShowMessage("Der Name des Kindes ist leer oder mit Leerzeichen versehen.",
                    "Bitte geben Sie den Namen eines Kindes an.");
                return null;
                }

                if (string.IsNullOrWhiteSpace(homeFolder))
                {
                    throw new ArgumentException("Home folder path is null or empty.", nameof(homeFolder));
                }
                if (string.IsNullOrWhiteSpace(groupDropdownText))
                {
                    throw new ArgumentException("Group dropdown text is null or empty.", nameof(groupDropdownText));
                }

                var groupFolder = ConvertSpecialCharacters(groupDropdownText, ConversionType.Umlaute);

                var groupPath = $@"{homeFolder}\Entwicklungsberichte\{groupFolder} Entwicklungsberichte\Aktuell";

                if (!Directory.Exists(groupPath))
                {
                    LogAndShowMessage($"Gruppenpfad existiert nicht: {groupPath}",
                        $"Der Pfad für den Gruppenordner {groupFolder} ist nicht zugänglich. Bitte überprüfen Sie den Pfad und versuchen Sie es erneut.");
                    return null;
                }

                var directories = await Task.Run(() => Directory.GetDirectories(groupPath));
                var kidNameExists = directories.Any(dir =>
                    dir.Split(Path.DirectorySeparatorChar).Last().Equals(kidName, StringComparison.OrdinalIgnoreCase));

                if (kidNameExists) return kidName;
                LogAndShowMessage($"Name des Kindes nicht im Gruppenverzeichnis gefunden: {kidName}",
                    "Der Name des Kindes wurde im Gruppenverzeichnis nicht gefunden. Bitte geben Sie einen gültigen Namen an.");
                    return null;
                }
                catch (Exception ex)
                {
                LogException(ex, $"Error validating kid name: {ex.Message}");
                return null;
            }
        }

        public static int? ValidateReportYearFromTextbox(string reportYearText)
        {
            if (string.IsNullOrWhiteSpace(reportYearText)) return null;

            var isValidYear = int.TryParse(reportYearText, out var parsedYear) && parsedYear is >= 2023 and <= 2099;

            if (!isValidYear)
                throw new Exception(
                    "Das Jahr muss aus genau 4 Ziffern bestehen, und zwischen 2023 und 2099 liegen. Bitte geben Sie ein gültiges Jahr ein.");

            return parsedYear;
        }
    }
}