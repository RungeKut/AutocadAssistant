using AutoCAD;
using System;
using System.Windows.Forms;

namespace AutocadAssistant
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string blockNameFrom = textBox1.Text.Trim();
            string blockNameTo = textBox2.Text.Trim();

            if (string.IsNullOrEmpty(blockNameFrom) || string.IsNullOrEmpty(blockNameTo))
            {
                MessageBox.Show("Введите имена обоих блоков.");
                return;
            }

            try
            {
                ReplaceBlocksInActiveDrawing(blockNameFrom, blockNameTo);
                MessageBox.Show("Замена завершена успешно!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void ReplaceBlocksInActiveDrawing(string blockNameFrom, string blockNameTo)
        {
            string[] newBlockNames = blockNameTo.Trim(' ').Split(',');
            // Получаем COM-объект AutoCAD
            AcadApplication acadApp = null;
            try
            {
                acadApp = (AcadApplication)System.Runtime.InteropServices.Marshal.GetActiveObject("AutoCAD.Application");
            }
            catch
            {
                throw new Exception("AutoCAD не запущен или не найден.");
            }

            var doc = acadApp.ActiveDocument;
            var db = doc.Database;
            var modelSpace = (AcadBlock)doc.ModelSpace;

            // Собираем все блоки с именем blockNameFrom
            foreach (var entity in modelSpace)
            {
                if (entity is AcadBlockReference blockRef)
                {
                    if (blockRef.Name.Equals(blockNameFrom, StringComparison.OrdinalIgnoreCase))
                    {
                        // Сохраняем параметры
                        var insertionPoint = blockRef.InsertionPoint;
                        var rotation = blockRef.Rotation;
                        var scaleX = blockRef.XScaleFactor;
                        var scaleY = blockRef.YScaleFactor;
                        var scaleZ = blockRef.ZScaleFactor;
                        var layer = blockRef.Layer;

                        
                        

                        // Удаляем старый блок
                        blockRef.Delete();

                        foreach (var block in newBlockNames)
                        {
                            // Вставляем новый блок с теми же параметрами
                            var newBlock = modelSpace.InsertBlock(insertionPoint, block, scaleX, scaleY, scaleZ, rotation);
                            newBlock.Layer = layer;
                        }
                    }
                }
            }

            // Удаляем определение блока blockNameTo из таблицы блоков (если нужно)
            // ⚠️ Осторожно: убедитесь, что блок больше не используется!
            try
            {
                var blockObj = db.Blocks.Item(blockNameTo);
                if (blockObj != null)
                {
                    // Проверим, есть ли ещё ссылки на этот блок
                    bool hasReferences = false;
                    foreach (var ent in modelSpace)
                    {
                        if (ent is AcadBlockReference br &&
                            br.Name.Equals(blockNameTo, StringComparison.OrdinalIgnoreCase))
                        {
                            hasReferences = true;
                            break;
                        }
                    }

                    if (!hasReferences)
                    {
                        blockObj.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                // Блок может не существовать или быть вложенным — игнорируем
                System.Diagnostics.Debug.WriteLine($"Не удалось удалить определение блока {blockNameTo}: {ex.Message}");
            }
        }
    }
}
