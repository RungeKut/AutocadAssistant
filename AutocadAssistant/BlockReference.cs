using System;
using System.Collections.Generic;
using System.Linq;

namespace AutocadAssistant
{
    /// <summary>
    /// Объект - Вхождение блока
    /// </summary>
    public class BlockReference
    {
        public string Name { get; set; }
        public string EffectiveName { get; set; }
        public double[] Point { get; set; }
        public double XPoint { get { return Point[0]; } }
        public double YPoint { get { return Point[1]; } }
        public double ZPoint { get { return Point[2]; } }
        public double Rotation { get; set; }
        public double XScaleFactor { get; set; }
        public double YScaleFactor { get; set; }
        public double ZScaleFactor { get; set; }
        public string Layer { get; set; }
        public string[] points { get; set; }

        /// <summary>
        /// Сравнение координат блоков с заданной точностью, метров
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static bool ArePointsEqual(BlockReference a, BlockReference b, double tolerance = 0.2)
        {
            return Math.Abs(a.XPoint - b.XPoint) <= tolerance &&
                   Math.Abs(a.YPoint - b.YPoint) <= tolerance &&
                   Math.Abs(a.ZPoint - b.ZPoint) <= tolerance;
        }

        /// <summary>
        /// Вычисляет расстояние между двумя точками (2D)
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        private double Distance2D(double[] p1, double[] p2)
        {
            return Math.Sqrt(Math.Pow(p1[0] - p2[0], 2) + Math.Pow(p1[1] - p2[1], 2));
        }

        /// <summary>
        /// Проверяет, находится ли точка в пределах прямоугольной зоны
        /// </summary>
        /// <param name="point"></param>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <returns></returns>
        private bool IsPointInBox(double[] point, double minX, double minY, double maxX, double maxY)
        {
            return point[0] >= minX && point[0] <= maxX && point[1] >= minY && point[1] <= maxY;
        }

        /// <summary>
        /// Удаление дубликатов и нежелательных блоков
        /// </summary>
        /// <param name="blocks"></param>
        /// <param name="unwantedBlockName"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static List<BlockReference> RemoveDuplicateAndUnwantedBlocks(List<BlockReference> blocks, string unwantedBlockName, double tolerance = 1.0)
        {
            if (blocks == null || !blocks.Any())
                return new List<BlockReference>();

            var result = new List<BlockReference>();
            var processed = new bool[blocks.Count];

            for (int i = 0; i < blocks.Count; i++)
            {
                if (processed[i]) continue;

                var current = blocks[i];
                var group = new List<BlockReference> { current };

                // Находим все блоки в той же точке
                for (int j = i + 1; j < blocks.Count; j++)
                {
                    if (!processed[j] && ArePointsEqual(current, blocks[j], tolerance))
                    {
                        group.Add(blocks[j]);
                        processed[j] = true;
                    }
                }

                processed[i] = true;

                // Обрабатываем группу
                if (group.Count == 1)
                {
                    // Один блок — добавляем
                    result.Add(current);
                }
                else
                {
                    // Несколько блоков в одной точке
                    var unwantedExists = group.Any(b => b.Name == unwantedBlockName);
                    var wantedBlocks = group.Where(b => b.Name != unwantedBlockName).ToList();

                    if (wantedBlocks.Count == 1)
                    {
                        // Один желаемый — оставляем его
                        result.Add(wantedBlocks[0]);
                    }
                    else if (wantedBlocks.Count > 1)
                    {
                        // Несколько "хороших" — оставляем один (например, первый)
                        result.Add(wantedBlocks[0]);
                    }
                    else
                    {
                        // Все — нежелательные (маловероятно, но...)
                        // Оставляем один, чтобы не потерять точку
                        result.Add(group[0]);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Похож ли текст на отметку высоты
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private bool IsHeightMark(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string clean = text.ToUpperInvariant();

            // Частые ключевые слова в отметках
            string[] keywords = { "ОТМ", "EL", "▽", "Δ", "H=", "H =" };
            if (keywords.Any(kw => clean.Contains(kw)))
                return true;

            // Убираем всё, кроме цифр, знаков и точки/запятой
            string numericPart = new string(text.Where(c => char.IsDigit(c) || c == '.' || c == ',' || c == '+' || c == '-').ToArray());

            // Должно быть хотя бы 2 цифры и одна точка/запятая (или просто число >= 3 знаков)
            if (numericPart.Length >= 3 && numericPart.Any(char.IsDigit))
            {
                // Проверим, можно ли распарсить как число (осторожно: локаль!)
                if (double.TryParse(numericPart.Replace(',', '.'), out _))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Фильтрация вертикального кластера
        /// </summary>
        /// <param name="texts"></param>
        /// <param name="maxHorizontalDeviation"></param>
        /// <returns></returns>
        private List<dynamic> FilterVerticalCluster(List<dynamic> texts, double maxHorizontalDeviation = 100.0)
        {
            if (texts.Count <= 1)
                return texts;

            // Средняя X-координата
            double avgX = texts.Average(t =>
            {
                var pos = (t.ObjectName == "AcDbText") ? t.InsertionPoint : t.InsertionPoint;
                return pos[0];
            });

            // Оставляем только те, что в пределах ±maxHorizontalDeviation по X
            return texts.Where(t =>
            {
                var pos = (t.ObjectName == "AcDbText") ? t.InsertionPoint : t.InsertionPoint;
                return Math.Abs(pos[0] - avgX) <= maxHorizontalDeviation;
            }).ToList();
        }

        /// <summary>
        /// Поиск отметок в радиусе вокруг блока
        /// </summary>
        /// <param name="blockRef"></param>
        /// <param name="modelSpace"></param>
        /// <param name="searchRadius"></param>
        /// <returns></returns>
        private List<dynamic> FindHeightTextsAroundBlock(dynamic blockRef, dynamic modelSpace, double searchRadius = 10.0) // радиус в единицах чертежа (мм или м)
        {
            var texts = new List<dynamic>();
            double[] blockPos = blockRef.InsertionPoint;

            foreach (dynamic ent in modelSpace)
            {
                string objName = ent.ObjectName;

                // Пропускаем всё, кроме текста
                if (objName != "AcDbText" && objName != "AcDbMText")
                    continue;

                // Определяем позицию текста
                double[] textPos;
                if (objName == "AcDbText")
                {
                    textPos = ent.InsertionPoint;
                }
                else // AcDbMText
                {
                    // MText может иметь разные точки привязки,
                    // но InsertionPoint — основная для позиционирования
                    textPos = ent.InsertionPoint;
                }

                // Проверяем расстояние
                if (Distance2D(blockPos, textPos) > searchRadius)
                    continue;

                // Проверяем, похож ли текст на отметку высоты
                string textStr = ent.TextString?.ToString().Trim() ?? "";
                if (IsHeightMark(textStr))
                {
                    texts.Add(ent);
                }
            }

            // Сортируем по Y (сверху вниз)
            texts = texts.OrderBy(t =>
            {
                var pos = (t.ObjectName == "AcDbText") ? t.InsertionPoint : t.InsertionPoint;
                return -pos[1]; // убывающий Y → сверху вниз
            }).ToList();

            // Опционально: оставляем только вертикально выстроенные
            // (если нужно — иначе удалите эту строку)
            texts = FilterVerticalCluster(texts, maxHorizontalDeviation: searchRadius * 0.3);

            return texts;
        }
    }
}
