﻿using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using static Automatisiertes_Kopieren.LoggingService;

namespace Automatisiertes_Kopieren
{
    public class FileManager
    {
        private readonly string _homeFolder;
        private readonly static LoggingService _loggingService = new LoggingService();

        public FileManager(string homeFolder)
        {
            _homeFolder = homeFolder ?? throw new ArgumentNullException(nameof(homeFolder));
        }

        public string GetTargetPath(string group, string kidName, string reportYear, string reportMonth)
        {
            group = StringUtilities.ConvertToTitleCase(group);
            group = StringUtilities.ConvertSpecialCharacters(group, StringUtilities.ConversionType.Umlaute);

            kidName = StringUtilities.ConvertToTitleCase(kidName);

            if (string.IsNullOrEmpty(_homeFolder))
            {
                throw new InvalidOperationException("Das Hauptverzeichnis ist nicht festgelegt.");
            }
            return $@"{_homeFolder}\Entwicklungsberichte\{group} Entwicklungsberichte\Aktuell\{kidName}\{reportYear}\{reportMonth}";
        }

        public bool SafeRenameFile(string sourceFile, string destFile)
        {
            try
            {
                if (File.Exists(destFile))
                {
                    MessageBoxResult result = _loggingService.ShowMessage("Die Datei existiert bereits. Möchtest du diese ersetzen?",
                        LoggingService.MessageType.Info,
                        "Confirm Replace",
                        MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.No)
                    {
                        return false; // User chose not to replace the file.
                    }
                    File.Delete(destFile);
                }

                File.Move(sourceFile, destFile);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogAndShowMessage($"Fehler beim Umbenennen der Datei: {ex.Message}", "Fehler beim Umbenennen der Datei", LoggingService.LogLevel.Error, LoggingService.MessageType.Error);
                return false;
            }
        }


        public Tuple<string, string, string, string> RenameFilesInTargetDirectory(string targetFolderPath, string kidName, string reportMonth, string reportYear, bool isAllgemeinerChecked, bool isVorschulChecked, bool isProtokollbogenChecked, string protokollNumber)
        {
            string? renamedProtokollbogenPath = null;
            string? renamedAllgemeinEntwicklungsberichtPath = null;
            string? renamedProtokollElterngespraechPath = null;
            string? renamedVorschulEntwicklungsberichtPath = null;
            kidName = StringUtilities.ConvertToTitleCase(kidName);
            kidName = StringUtilities.ConvertSpecialCharacters(kidName, StringUtilities.ConversionType.Umlaute, StringUtilities.ConversionType.Underscore);

            reportMonth = StringUtilities.ConvertToTitleCase(reportMonth);
            reportMonth = StringUtilities.ConvertSpecialCharacters(reportMonth, StringUtilities.ConversionType.Umlaute, StringUtilities.ConversionType.Underscore);
            int numericProtokollNumber;
            if (!int.TryParse(Regex.Match(protokollNumber, @"\d+").Value, out numericProtokollNumber))
            {
                _loggingService.LogMessage($"Failed to extract numeric value from protokollNumber: {protokollNumber}", LogLevel.Error);
                return new Tuple<string, string, string, string>(renamedProtokollbogenPath ?? string.Empty, renamedAllgemeinEntwicklungsberichtPath ?? string.Empty, renamedProtokollElterngespraechPath ?? string.Empty, renamedVorschulEntwicklungsberichtPath ?? string.Empty);
            }


            string[] files = Directory.GetFiles(targetFolderPath);

            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string fileExtension = Path.GetExtension(file);

                if (fileName.Equals("Allgemeiner-Entwicklungsbericht", StringComparison.OrdinalIgnoreCase) && isAllgemeinerChecked)
                {
                    string newFileName = $"{kidName}_Allgemeiner_Entwicklungsbericht_{reportMonth}_{reportYear}{fileExtension}";
                    SafeRenameFile(file, Path.Combine(targetFolderPath, newFileName));
                    renamedAllgemeinEntwicklungsberichtPath = Path.Combine(targetFolderPath, newFileName);
                }

                if (fileName.Equals("Vorschul-Entwicklungsbericht", StringComparison.OrdinalIgnoreCase) && isVorschulChecked)
                {
                    string newFileName = $"{kidName}_Vorschul_Entwicklungsbericht_{reportMonth}_{reportYear}{fileExtension}";
                    SafeRenameFile(file, Path.Combine(targetFolderPath, newFileName));
                    renamedVorschulEntwicklungsberichtPath = Path.Combine(targetFolderPath, newFileName);
                }


                if (fileName.StartsWith("Kind_Protokollbogen_", StringComparison.OrdinalIgnoreCase) && isProtokollbogenChecked)
                {
                    string newFileName = $"{kidName}_{protokollNumber}_Protokollbogen_{reportMonth}_{reportYear}{fileExtension}";
                    SafeRenameFile(file, Path.Combine(targetFolderPath, newFileName));
                    renamedProtokollbogenPath = Path.Combine(targetFolderPath, newFileName);
                }

                if (fileName.Equals("Protokoll-Elterngespraech", StringComparison.OrdinalIgnoreCase))
                {
                    string newFileName = $"{kidName}_Protokoll_Elterngespraech_{reportMonth}_{reportYear}{fileExtension}";
                    SafeRenameFile(file, Path.Combine(targetFolderPath, newFileName));
                    renamedProtokollElterngespraechPath = Path.Combine(targetFolderPath, newFileName);
                }
            }
            return new Tuple<string, string, string, string>(renamedProtokollbogenPath ?? string.Empty, renamedAllgemeinEntwicklungsberichtPath ?? string.Empty, renamedProtokollElterngespraechPath ?? string.Empty, renamedVorschulEntwicklungsberichtPath ?? string.Empty);
        }

        public static class StringUtilities
        {
            public static string ConvertToTitleCase(string inputString)
            {
                if (string.IsNullOrWhiteSpace(inputString))
                    return string.Empty;

                TextInfo textInfo = new CultureInfo("de-DE", false).TextInfo;
                return textInfo.ToTitleCase(inputString.ToLower());
            }

            public static string ConvertSpecialCharacters(string input, params ConversionType[] types)
            {
                foreach (var type in types)
                {
                    switch (type)
                    {
                        case ConversionType.Umlaute:
                            input = input.Replace("ä", "ae").Replace("ö", "oe");
                            break;
                        case ConversionType.Underscore:
                            input = input.Replace(" ", "_");
                            break;
                    }
                }
                return input;
            }

            public enum ConversionType
            {
                Umlaute,
                Underscore
            }
        }

        public void CopyFilesFromSourceToTarget(string? sourceFile, string targetFolderPath, string protokollbogenFileName)
        {
            if (!Directory.Exists(targetFolderPath))
            {
                Directory.CreateDirectory(targetFolderPath);
            }

            if (sourceFile != null && File.Exists(sourceFile))
            {
                try
                {
                    SafeCopyFile(sourceFile, Path.Combine(targetFolderPath, protokollbogenFileName));
                }
                catch (Exception ex)
                {
                    _loggingService.LogMessage($"Error copying file. Source: {sourceFile}, Destination: {Path.Combine(targetFolderPath, protokollbogenFileName)}. Error: {ex.Message}", LoggingService.LogLevel.Error);
                }
            }
            else
            {
                _loggingService.LogMessage($"File {protokollbogenFileName} not found in source folder.", LogLevel.Warning);
            }
        }

        public void SafeCopyFile(string sourceFile, string destFile)
        {
            try
            {
                if (File.Exists(destFile))
                {
                    MessageBoxResult result = _loggingService.ShowMessage("Möchten Sie das Hauptverzeichnis ändern?", MessageType.Info, "Hauptverzeichnis nicht festgelegt", MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        string backupFilename = $"{Path.GetDirectoryName(destFile)}\\{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(destFile)}.bak";
                        File.Move(destFile, backupFilename);
                        _loggingService.ShowMessage($"Die vorhandene Datei wurde gesichert als: {backupFilename}", LoggingService.MessageType.Info, "Info");
                    }
                    else
                    {
                        _loggingService.ShowMessage("Die Datei wurde nicht kopiert.", LoggingService.MessageType.Info, "Info");
                        return;
                    }
                }

                File.Copy(sourceFile, destFile, overwrite: true);
            }
            catch (Exception ex)
            {
                _loggingService.LogAndShowMessage($"Fehler beim Kopieren der Datei: {ex.Message}", "Fehler beim Kopieren der Datei", LoggingService.LogLevel.Error, LoggingService.MessageType.Error);
            }
        }

    }
}
