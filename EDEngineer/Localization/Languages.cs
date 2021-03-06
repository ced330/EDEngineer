﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using EDEngineer.DesignTime;
using EDEngineer.Models;
using EDEngineer.Models.Utils;
using EDEngineer.Properties;
using EDEngineer.Utils.System;
using Newtonsoft.Json;

namespace EDEngineer.Localization
{
    public class Languages : IValueConverter, IMultiValueConverter, ILanguage
    {
        public static Languages Instance => instance ?? (instance = InitLanguages());

        public LanguageInfo DefaultLanguage => LanguageInfos[DEFAULT_LANG];

        private const string DEFAULT_LANG = "en";

        private LanguageInfo currentLanguage;
        private static Languages instance;
        public IDictionary<string, LanguageInfo> LanguageInfos { get; set; }
        public Dictionary<string, Translation> Translations { get; set; }

        public LanguageInfo CurrentLanguage
        {
            get => currentLanguage;
            set
            {
                currentLanguage = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static Languages InitLanguages()
        {
            if ((bool)DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue)
            {
                return new Languages();
            }

            var json = Helpers.GetLocalizationJson();
            var languages = JsonConvert.DeserializeObject<Languages>(json);

            var total = CheckForExistingTranslation(languages);
            ComputeProgress(languages, total);

            if (string.IsNullOrEmpty(Settings.Default.Language))
            {
                languages.CurrentLanguage = languages.LanguageInfos[DEFAULT_LANG];
                PromptLanguage(languages);
            }
            else
            {
                languages.CurrentLanguage = languages.LanguageInfos[Settings.Default.Language];
            }

            return languages;
        }

        public static void PromptLanguage(Languages languages)
        {
            new Views.Popups.SelectLanguageWindow(languages).ShowDialog();
            Settings.Default.Language = languages.CurrentLanguage.TwoLetterISOLanguageName;
            Settings.Default.Save();
        }

        private static double CheckForExistingTranslation(Languages languages)
        {
            var total = 0d;
            foreach (var text in languages.Translations.Keys)
            {
                total++;

                var translation = languages.Translations[text];
                foreach (var lang in translation.Keys)
                {
                    var translatedText = translation[lang];
                    if (translatedText != null)
                    {
                        languages.LanguageInfos[lang].Progress += 1;
                    }
                }
            }
            return total;
        }

        private static void ComputeProgress(Languages languages, double total)
        {
            foreach (var lang in languages.LanguageInfos.Keys)
            {
                if (lang == DEFAULT_LANG)
                {
                    languages.LanguageInfos[lang].Progress = 1;
                }
                else
                {
                    languages.LanguageInfos[lang].Progress /= total;
                }

                languages.LanguageInfos[lang].Progress *= 100;

                if (languages.LanguageInfos[lang].Progress == 100)
                {
                    languages.LanguageInfos[lang].Ready = true;
                }
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DesignLanguageInfo)
            {
                return parameter;
            }

            if (value == null)
            {
                return null;
            }

            LanguageInfo lang;
            string text;

            if (parameter == null)
            {
                lang = currentLanguage;
                text = value.ToString();
            }
            else
            {
                lang = value as LanguageInfo;

                if (lang == null)
                {
                    return null;
                }

                text = parameter.ToString();
            }

#if DEBUG
            /*if (!Translations.ContainsKey(text) && text != "Konnga Ale" && text != "Lavian Brandy" && text != "Xihe Companions" && text != "Fujin Tea" && text != "Kamitra Cigars" && text != "Painite" && text != "Gold" && text != "Landmines" && text != "Meta-alloys" && text != "Soontill Relics")
            {
                // MessageBox.Show($"No localization for text : {text}");
            }*/
#endif

            if (!Translations.ContainsKey(text) || !Translations[text].TryGetValue(lang.TwoLetterISOLanguageName, out var translatedText) || string.IsNullOrEmpty(translatedText))
            {
                translatedText = text;
            }

            return translatedText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(values[1], targetType, values[0], culture);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public string Translate(string text)
        {
            return (string) Convert(CurrentLanguage, null, text, null);
        }

        public string Translate(string text, LanguageInfo langInfo)
        {
            return (string) Convert(langInfo, null, text, null);
        }

        public bool TryGetLangInfo(string lang, out LanguageInfo langInfo)
        {
            return LanguageInfos.TryGetValue(lang, out langInfo);
        }
    }
}