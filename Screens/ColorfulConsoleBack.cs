﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace Colorful
{
    /// <summary>
    /// Wraps around the System.Console class, adding enhanced styling functionality.
    /// </summary>
    public static partial class Console
    {
        public static ColorStore colorStore;
        private static ColorManagerFactory colorManagerFactory;
        public static ColorManager colorManager;
        private static Dictionary<string, COLORREF> defaultColorMap;

        // Limitation of the Windows console window.
        private const int MAX_COLOR_CHANGES = 16;

        // Note that if you set ConsoleColor.Black to a different color, then the background of the
        // console will change as a side-effect!  The index of Black (in the ConsoleColor definition) is 0,
        // so avoid that index.
        private const int INITIAL_COLOR_CHANGE_COUNT_VALUE = 1;

        private static readonly string WRITELINE_TRAILER = "\r\n";
        private static readonly string WRITE_TRAILER = "";

        private static void MapToScreen(IEnumerable<KeyValuePair<string, Color>> styleMap, string trailer)
        {
            int writeCount = 1;
            foreach (KeyValuePair<string, Color> textChunk in styleMap)
            {
                System.Console.BackgroundColor = colorManager.GetConsoleColor(textChunk.Value);

                if (writeCount == styleMap.Count())
                {
                    System.Console.Write(textChunk.Key + trailer);
                }
                else
                {
                    System.Console.Write(textChunk.Key);
                }

                writeCount++;
            }

            System.Console.ResetColor();
        }

        private static void MixMapToScreen(IEnumerable<KeyValuePair<string, Color>> styleMap, IEnumerable<KeyValuePair<string, Color>> backStyleMap, string trailer)
        {
            int writeCount = 1;
            var StyleArray = styleMap as KeyValuePair<string, Color>[] ?? styleMap.ToArray();
            var StyleArray2 = backStyleMap as KeyValuePair<string, Color>[] ?? backStyleMap.ToArray();

            for (int o = 0; o != StyleArray.Length; o++)
            {
                KeyValuePair<string, Color> textChunk = StyleArray[o];
                KeyValuePair<string, Color> textChunk2 = StyleArray2[o];

                ForegroundColor = textChunk.Value;
                if(!textChunk2.Value.IsEmpty)
                if (textChunk2.Value.Name != Color.Empty.Name)
                if (BackgroundColor.Name != textChunk2.Value.Name)
                BackgroundColor = textChunk2.Value;

                if (writeCount == StyleArray.Count())
                {
                    System.Console.Write(textChunk.Key + trailer);
                }
                else
                {
                    System.Console.Write(textChunk.Key);
                }

                writeCount++;
            }

            //System.Console.ResetColor();
        }

        private static void MapToScreen(StyledString styledString, string trailer)
        {
            int rowLength = styledString.CharacterGeometry.GetLength(dimension: 0);
            int columnLength = styledString.CharacterGeometry.GetLength(dimension: 1);
            for (int row = 0; row < rowLength; row++)
            {
                for (int column = 0; column < columnLength; column++)
                {
                    System.Console.ForegroundColor = colorManager.GetConsoleColor(styledString.ColorGeometry[row, column]);

                    if (row == rowLength - 1 && column == columnLength - 1)
                    {
                        System.Console.Write(styledString.CharacterGeometry[row, column] + trailer);
                    }
                    else if (column == columnLength - 1)
                    {
                        System.Console.Write(styledString.CharacterGeometry[row, column] + "\r\n");
                    }
                    else
                    {
                        System.Console.Write(styledString.CharacterGeometry[row, column]);
                    }
                }
            }

            System.Console.ResetColor();
        }

        private static void WriteInColor<T>(Action<T> action, T target, Color color)
        {
            System.Console.ForegroundColor = colorManager.GetConsoleColor(color);
            action.Invoke(target);
            System.Console.ResetColor();
        }

        private static void WriteChunkInColor(Action<string> action, char[] buffer, int index, int count, Color color)
        {
            string chunk = buffer.AsString().Substring(index, count);

            WriteInColor(action, chunk, color);
        }

        private static void WriteInColorAlternating<T>(Action<T> action, T target, ColorAlternator alternator)
        {
            Color color = alternator.GetNextColor(target.AsString());

            System.Console.ForegroundColor = colorManager.GetConsoleColor(color);
            action.Invoke(target);
            System.Console.ResetColor();
        }

        private static void WriteChunkInColorAlternating(Action<string> action, char[] buffer, int index, int count, ColorAlternator alternator)
        {
            string chunk = buffer.AsString().Substring(index, count);

            WriteInColorAlternating(action, chunk, alternator);
        }

        private static void WriteInColorStyled<T>(string trailer, T target, StyleSheet styleSheet)
        {
            TextAnnotator annotator = new TextAnnotator(styleSheet);
            List<KeyValuePair<string, Color>> annotationMap = annotator.GetAnnotationMap(target.AsString());

            MapToScreen(annotationMap, trailer);
        }

        private static void WriteAsciiInColorStyled(string trailer, StyledString target, StyleSheet styleSheet)
        {
            TextAnnotator annotator = new TextAnnotator(styleSheet);
            List<KeyValuePair<string, Color>> annotationMap = annotator.GetAnnotationMap(target.AbstractValue); // Should eventually be target.AsStyledString() everywhere...?

            PopulateColorGeometry(annotationMap, target);

            MapToScreen(target, trailer);
        }

        private static void PopulateColorGeometry(IEnumerable<KeyValuePair<string, Color>> annotationMap, StyledString target)
        {
            int abstractCharCount = 0;
            foreach (KeyValuePair<string, Color> fragment in annotationMap)
            {
                for (int i = 0; i < fragment.Key.Length; i++)
                {
                    // This will run O(n^2) times...but with DP, could be O(n).
                    // Just need to keep a third array that keeps track of each abstract char's width, so you never iterate past that.
                    // This third array would be one-dimensional.

                    int rowLength = target.CharacterIndexGeometry.GetLength(dimension: 0);
                    int columnLength = target.CharacterIndexGeometry.GetLength(dimension: 1);
                    for (int row = 0; row < rowLength; row++)
                    {
                        for (int column = 0; column < columnLength; column++)
                        {
                            if (target.CharacterIndexGeometry[row, column] == abstractCharCount)
                            {
                                target.ColorGeometry[row, column] = fragment.Value;
                            }
                        }
                    }

                    abstractCharCount++;
                }
            }
        }

        private static void WriteChunkInColorStyled(string trailer, char[] buffer, int index, int count, StyleSheet styleSheet)
        {
            string chunk = buffer.AsString().Substring(index, count);

            WriteInColorStyled(trailer, chunk, styleSheet);
        }

        private static void WriteInColor<T, U>(Action<T, U> action, T target0, U target1, Color color)
        {
            System.Console.ForegroundColor = colorManager.GetConsoleColor(color);
            action.Invoke(target0, target1);
            System.Console.ResetColor();
        }

        private static void WriteInColorAlternating<T, U>(Action<T, U> action, T target0, U target1, ColorAlternator alternator)
        {
            string formatted = string.Format(target0.ToString(), target1.Normalize());
            Color color = alternator.GetNextColor(formatted);

            System.Console.ForegroundColor = colorManager.GetConsoleColor(color);
            action.Invoke(target0, target1);
            System.Console.ResetColor();
        }

        private static void WriteInColorStyled<T, U>(string trailer, T target0, U target1, StyleSheet styleSheet)
        {
            TextAnnotator annotator = new TextAnnotator(styleSheet);

            string formatted = string.Format(target0.ToString(), target1.Normalize());
            List<KeyValuePair<string, Color>> annotationMap = annotator.GetAnnotationMap(formatted);

            MapToScreen(annotationMap, trailer);
        }

        private static void WriteInColorFormatted<T, U>(string trailer, T target0, U target1, Color styledColor, Color defaultColor)
        {
            TextFormatter formatter = new TextFormatter(defaultColor);
            List<KeyValuePair<string, Color>> formatMap = formatter.GetFormatMap(target0.ToString(), target1.Normalize(), new Color[] { styledColor });

            MapToScreen(formatMap, trailer);
        }

        private static void WriteInColorFormatted<T>(string trailer, T target0, Formatter target1, Color defaultColor)
        {
            TextFormatter formatter = new TextFormatter(defaultColor);
            List<KeyValuePair<string, Color>> formatMap = formatter.GetFormatMap(target0.ToString(), new object[] { target1.Target }, new Color[] { target1.Color });

            MapToScreen(formatMap, trailer);
        }

        private static void WriteInColor<T, U>(Action<T, U, U> action, T target0, U target1, U target2, Color color)
        {
            System.Console.ForegroundColor = colorManager.GetConsoleColor(color);
            action.Invoke(target0, target1, target2);
            System.Console.ResetColor();
        }

        private static void WriteInColorAlternating<T, U>(Action<T, U, U> action, T target0, U target1, U target2, ColorAlternator alternator)
        {
            string formatted = string.Format(target0.ToString(), target1, target2); // NOT FORMATTING
            Color color = alternator.GetNextColor(formatted);

            System.Console.ForegroundColor = colorManager.GetConsoleColor(color);
            action.Invoke(target0, target1, target2);
            System.Console.ResetColor();
        }

        private static void WriteInColorStyled<T, U>(string trailer, T target0, U target1, U target2, StyleSheet styleSheet)
        {
            TextAnnotator annotator = new TextAnnotator(styleSheet);

            string formatted = string.Format(target0.ToString(), target1, target2);
            List<KeyValuePair<string, Color>> annotationMap = annotator.GetAnnotationMap(formatted);

            MapToScreen(annotationMap, trailer);
        }

        private static void WriteInColorFormatted<T, U>(string trailer, T target0, U target1, U target2, Color styledColor, Color defaultColor)
        {
            TextFormatter formatter = new TextFormatter(defaultColor);
            List<KeyValuePair<string, Color>> formatMap = formatter.GetFormatMap(target0.ToString(), new U[] { target1, target2 }.Normalize(), new Color[] { styledColor });

            MapToScreen(formatMap, trailer);
        }

        private static void WriteInColorFormatted<T>(string trailer, T target0, Formatter target1, Formatter target2, Color defaultColor)
        {
            TextFormatter formatter = new TextFormatter(defaultColor);
            List<KeyValuePair<string, Color>> formatMap = formatter.GetFormatMap(target0.ToString(), new object[] { target1.Target, target2.Target }, new Color[] { target1.Color, target2.Color });

            MapToScreen(formatMap, trailer);
        }

        private static void WriteInColor<T, U>(Action<T, U, U, U> action, T target0, U target1, U target2, U target3, Color color)
        {
            System.Console.ForegroundColor = colorManager.GetConsoleColor(color);
            action.Invoke(target0, target1, target2, target3);
            System.Console.ResetColor();
        }

        private static void WriteInColorAlternating<T, U>(Action<T, U, U, U> action, T target0, U target1, U target2, U target3, ColorAlternator alternator)
        {
            string formatted = string.Format(target0.ToString(), target1, target2, target3);
            Color color = alternator.GetNextColor(formatted);

            System.Console.ForegroundColor = colorManager.GetConsoleColor(color);
            action.Invoke(target0, target1, target2, target3);
            System.Console.ResetColor();
        }

        private static void WriteInColorStyled<T, U>(string trailer, T target0, U target1, U target2, U target3, StyleSheet styleSheet)
        {
            TextAnnotator annotator = new TextAnnotator(styleSheet);

            string formatted = string.Format(target0.ToString(), target1, target2, target3);
            List<KeyValuePair<string, Color>> annotationMap = annotator.GetAnnotationMap(formatted);

            MapToScreen(annotationMap, trailer);
        }

        private static void WriteInColorFormatted<T, U>(string trailer, T target0, U target1, U target2, U target3, Color styledColor, Color defaultColor)
        {
            TextFormatter formatter = new TextFormatter(defaultColor);
            List<KeyValuePair<string, Color>> formatMap = formatter.GetFormatMap(target0.ToString(), new U[] { target1, target2, target3 }.Normalize(), new Color[] { styledColor });

            MapToScreen(formatMap, trailer);
        }

        private static void WriteInColorFormatted<T>(string trailer, T target0, Formatter target1, Formatter target2, Formatter target3, Color defaultColor)
        {
            TextFormatter styler = new TextFormatter(defaultColor);
            List<KeyValuePair<string, Color>> formatMap = styler.GetFormatMap(target0.ToString(), new object[] { target1.Target, target2.Target, target3.Target }, new Color[] { target1.Color, target2.Color, target3.Color });

            MapToScreen(formatMap, trailer);
        }

        private static void WriteInMixColorFormatted<T>(string trailer, T target0, Formatter[] targets, Formatter[] backTargets, Color defaultColor)
        {
            TextFormatter styler = new TextFormatter(defaultColor);
            TextFormatter stylerBlack = new TextFormatter(BackgroundColor);
            List<KeyValuePair<string, Color>> formatMap = 
                styler.GetFormatMap(target0.ToString(), targets.Select(formatter => formatter.Target).ToArray(), targets.Select(formatter => formatter.Color).ToArray());
            List<KeyValuePair<string, Color>> format2Map =
                stylerBlack.GetFormatMap(target0.ToString(), backTargets.Select(formatter => formatter.Target).ToArray(), backTargets.Select(formatter => formatter.Color).ToArray());
            MixMapToScreen(formatMap, format2Map, trailer);
        }

        private static void WriteInColorFormatted<T>(string trailer, T target0, Formatter[] targets, Color defaultColor)
        {
            TextFormatter styler = new TextFormatter(defaultColor);
            List<KeyValuePair<string, Color>> formatMap = styler.GetFormatMap(target0.ToString(), targets.Select(formatter => formatter.Target).ToArray(), targets.Select(formatter => formatter.Color).ToArray());

            MapToScreen(formatMap, trailer);
        }

        private static void DoWithGradient<T>(Action<object, Color> writeAction, IEnumerable<T> input, Color startColor, Color endColor, int maxColorsInGradient)
        {
            GradientGenerator generator = new GradientGenerator();
            List<StyleClass<T>> gradient = generator.GenerateGradient(input, startColor, endColor, maxColorsInGradient);

            foreach (StyleClass<T> item in gradient)
            {
                writeAction(item.Target, item.Color);
            }
        }

        private static Figlet GetFiglet(FigletFont font = null)
        {
            if (font == null)
            {
                return new Figlet();
            }
            else
            {
                return new Figlet(font);
            }
        }

        private static readonly Color blackEquivalent = Color.FromArgb(red: 0, green: 0, blue: 0);
        private static readonly Color blueEquivalent = Color.FromArgb(red: 0, green: 0, blue: 255);
        private static readonly Color cyanEquivalent = Color.FromArgb(red: 0, green: 255, blue: 255);
        private static readonly Color darkBlueEquivalent = Color.FromArgb(red: 0, green: 0, blue: 128);
        private static readonly Color darkCyanEquivalent = Color.FromArgb(red: 0, green: 128, blue: 128);
        private static readonly Color darkGrayEquivalent = Color.FromArgb(red: 128, green: 128, blue: 128);
        private static readonly Color darkGreenEquivalent = Color.FromArgb(red: 0, green: 128, blue: 0);
        private static readonly Color darkMagentaEquivalent = Color.FromArgb(red: 128, green: 0, blue: 128);
        private static readonly Color darkRedEquivalent = Color.FromArgb(red: 128, green: 0, blue: 0);
        private static readonly Color darkYellowEquivalent = Color.FromArgb(red: 128, green: 128, blue: 0);
        private static readonly Color grayEquivalent = Color.FromArgb(red: 192, green: 192, blue: 192);
        private static readonly Color greenEquivalent = Color.FromArgb(red: 0, green: 255, blue: 0);
        private static readonly Color magentaEquivalent = Color.FromArgb(red: 255, green: 0, blue: 255);
        private static readonly Color redEquivalent = Color.FromArgb(red: 255, green: 0, blue: 0);
        private static readonly Color whiteEquivalent = Color.FromArgb(red: 255, green: 255, blue: 255);
        private static readonly Color yellowEquivalent = Color.FromArgb(red: 255, green: 255, blue: 0);

        private static ColorStore GetColorStore()
        {
            ConcurrentDictionary<Color, ConsoleColor> colorMap = new ConcurrentDictionary<Color, ConsoleColor>();
            ConcurrentDictionary<ConsoleColor, Color> consoleColorMap = new ConcurrentDictionary<ConsoleColor, Color>();

            consoleColorMap.TryAdd(ConsoleColor.Black, blackEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.Blue, blueEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.Cyan, cyanEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.DarkBlue, darkBlueEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.DarkCyan, darkCyanEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.DarkGray, darkGrayEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.DarkGreen, darkGreenEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.DarkMagenta, darkMagentaEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.DarkRed, darkRedEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.DarkYellow, darkYellowEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.Gray, grayEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.Green, greenEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.Magenta, magentaEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.Red, redEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.White, whiteEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.Yellow, yellowEquivalent);

            return new ColorStore(colorMap, consoleColorMap);
        }

        private static void ReplaceAllColorsWithDefaults(bool isInCompatibilityMode)
        {
            colorStore = GetColorStore();
            colorManagerFactory = new ColorManagerFactory();
            colorManager = colorManagerFactory.GetManager(colorStore, MAX_COLOR_CHANGES, INITIAL_COLOR_CHANGE_COUNT_VALUE, isInCompatibilityMode);

            // There's no need to do this if in compatibility mode, as more than 16 colors won't be used, anyway.
            if (!colorManager.IsInCompatibilityMode)
            {
                new ColorMapper().SetBatchBufferColors(defaultColorMap);
            }
        }
    }
}
