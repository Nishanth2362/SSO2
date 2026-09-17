using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SSO.Application.Interfaces.Services;
using SSO.Common.Wrapper;
using System.Data;
using System.Reflection;

namespace SSO.Infrastructure.Services
{
    public class ExcelService : IExcelService
    {
        public async Task<string> ExportAsync<TData>(IEnumerable<TData> data, Dictionary<string, Func<TData, object>> mappers, string sheetName = "Sheet1")
        {
            using (var stream = new MemoryStream())
            {
                using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    var workbookPart = document.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();

                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    var sheetData = new SheetData();
                    worksheetPart.Worksheet = new Worksheet(sheetData);

                    var sheets = document.WorkbookPart.Workbook.AppendChild(new Sheets());
                    var sheet = new Sheet() { Id = document.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = sheetName };
                    sheets.Append(sheet);

                    // Add Header
                    var headerRow = new Row();
                    foreach (var key in mappers.Keys)
                    {
                        headerRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(key) });
                    }
                    sheetData.AppendChild(headerRow);

                    // Add Data
                    foreach (var item in data)
                    {
                        var row = new Row();
                        foreach (var mapper in mappers.Values)
                        {
                            var value = mapper(item)?.ToString() ?? string.Empty;
                            row.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(value) });
                        }
                        sheetData.AppendChild(row);
                    }

                    workbookPart.Workbook.Save();
                }

                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public async Task<IResult<IEnumerable<TEntity>>> ImportAsync<TEntity>(Stream data, Dictionary<string, Func<DataRow, TEntity, object>> mappers, string sheetName = "Sheet1")
        {
            try
            {
                var list = new List<TEntity>();
                var table = ReadSheetToDataTable(data, sheetName);

                foreach (DataRow row in table.Rows)
                {
                    TEntity entity = Activator.CreateInstance<TEntity>();
                    foreach (var mapper in mappers)
                    {
                        mapper.Value(row, entity);
                    }
                    list.Add(entity);
                }

                return await Result<IEnumerable<TEntity>>.SuccessAsync(list);
            }
            catch (Exception ex)
            {
                return await Result<IEnumerable<TEntity>>.FailAsync(ex.Message);
            }
        }

        private DataTable ReadSheetToDataTable(Stream data, string sheetName)
        {
            var dt = new DataTable();
            using (var document = SpreadsheetDocument.Open(data, false))
            {
                var workbookPart = document.WorkbookPart;
                var sheet = workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault(s => s.Name == sheetName);
                if (sheet == null) throw new Exception($"Sheet '{sheetName}' not found.");

                var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
                var sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
                var rows = sheetData.Elements<Row>().ToList();

                if (!rows.Any()) return dt;

                // Header Row
                var headerRow = rows.First();
                var columnIndices = new List<string>();
                foreach (var cell in headerRow.Elements<Cell>())
                {
                    var columnName = GetCellValue(document, cell);
                    dt.Columns.Add(columnName);
                    // Get the column reference (e.g., "A", "B")
                    var cellRef = cell.CellReference?.Value;
                    if (cellRef != null)
                    {
                        columnIndices.Add(new string(cellRef.Where(char.IsLetter).ToArray()));
                    }
                }

                // Data Rows
                foreach (var row in rows.Skip(1))
                {
                    var dr = dt.NewRow();
                    var cells = row.Elements<Cell>().ToList();

                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        var colRef = columnIndices[i];
                        var cell = cells.FirstOrDefault(c => c.CellReference?.Value?.StartsWith(colRef) == true);
                        if (cell != null)
                        {
                            dr[i] = GetCellValue(document, cell);
                        }
                    }
                    dt.Rows.Add(dr);
                }
            }
            return dt;
        }

        private string GetCellValue(SpreadsheetDocument document, Cell cell)
        {
            if (cell == null || cell.CellValue == null) return string.Empty;

            var value = cell.CellValue.InnerText;
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return document.WorkbookPart.SharedStringTablePart.SharedStringTable.ChildElements[int.Parse(value)].InnerText;
            }
            return value;
        }
    }
}
