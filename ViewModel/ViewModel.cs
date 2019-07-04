using InteractiveDataDisplay.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;

namespace CutBin
{
    public class InputProperty
    {
        public Label label;
        public Label units;
        public TextBox text;
        public CheckBox checkbox;
    }

    public class ComputedProperty
    {
        public Label label;
        public Label units;
        public TextBox text;
        public CutProperty model;
    }

    public class ViewModel
    {
        TreeView _cutTree;
        Grid _cutPropertyGrid;
        Grid _cutResultGrid;
        Grid _cutNameGrid;
        LineGraph _graph;
        TextBox _cutNameText;
        
        Dictionary<string, InputProperty> _inputFields;
        Dictionary<string, ComputedProperty> _computedFields;

        Cut _selectedCut;
        TreeViewItem _selectedItem;

        CutDB _cutDB;

        bool _refreshing;

        private static readonly Color k_disabledColor = Color.FromRgb(200, 200, 200);
        private static readonly Color k_computedColor = Color.FromRgb(200, 255, 200);
        private static readonly Color k_warningColor = Color.FromRgb(255, 200, 200);
        private static readonly Color k_enabledColor = Color.FromRgb(255, 255, 255);

        public ViewModel(MainWindow window)
        {
            _refreshing = true;

            _cutTree = window.cutTree;
            _cutPropertyGrid = window.cutPropertyGrid;
            _cutResultGrid = window.cutResultGrid;
            _cutNameGrid = window.cutNameGrid;
            _graph = window.linegraph;

            _inputFields = new Dictionary<string, InputProperty>();
            _computedFields = new Dictionary<string, ComputedProperty>();

            // The cut name textbox
            Label cutNameLabel = new Label();
            cutNameLabel.Content = "Name";
            cutNameLabel.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            _cutNameGrid.Children.Add(cutNameLabel);

            _cutNameText = new TextBox();
            _cutNameText.Text = "";
            _cutNameText.Margin = new System.Windows.Thickness(5);
            _cutNameText.TextChanged += (b, c) => CutNameChanged();
            _cutNameText.Background = new SolidColorBrush(k_enabledColor);
            _cutNameText.IsReadOnly = true;
            Grid.SetColumn(_cutNameText, 2);
            _cutNameGrid.Children.Add(_cutNameText);

            // build the input fields
            int row = 0;
            foreach (CutProperty property in Cut.GetCutPropertyDefinitions())
            {
                string name = property.name;

                RowDefinition rowDefinition = new RowDefinition();
                rowDefinition.Height = new GridLength(30);
                _cutPropertyGrid.RowDefinitions.Add(rowDefinition);

                InputProperty field = new InputProperty();
                _inputFields.Add(name, field);

                field.label = new Label();
                field.label.Content = name;
                field.label.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                Grid.SetRow(field.label, row);
                _cutPropertyGrid.Children.Add(field.label);
                
                field.text = new TextBox();
                field.text.Text = "";
                field.text.Margin = new System.Windows.Thickness(5);
                field.text.TextChanged += (b, c) => FieldTextChanged(name, field);
                field.text.Background = new SolidColorBrush(k_disabledColor);
                field.text.IsReadOnly = true;
                Grid.SetRow(field.text, row);
                Grid.SetColumn(field.text, 2);
                _cutPropertyGrid.Children.Add(field.text);

                field.checkbox = new CheckBox();
                field.checkbox.IsChecked = false;
                field.checkbox.Checked += (b, c) => FieldTextChecked(name, field);
                field.checkbox.Unchecked += (b, c) => FieldTextUnChecked(name, field);
                field.checkbox.Margin = new System.Windows.Thickness(5);
                Grid.SetRow(field.checkbox, row);
                Grid.SetColumn(field.checkbox, 1);
                _cutPropertyGrid.Children.Add(field.checkbox);

                field.units = new Label();
                field.units.Content = property.units;
                Grid.SetRow(field.units, row);
                Grid.SetColumn(field.units, 3);
                _cutPropertyGrid.Children.Add(field.units);

                row++;
            }

            // build the computed fields
            row = 0;
            foreach (CutProperty property in Cut.GetComputedPropertyDefinitions())
            {
                string name = property.name;
                ComputedProperty field = new ComputedProperty();
                _computedFields.Add(name, field);

                RowDefinition rowDefinition = new RowDefinition();
                rowDefinition.Height = new GridLength(30);
                _cutResultGrid.RowDefinitions.Add(rowDefinition);

                field.label = new Label();
                field.label.Content = name;
                field.label.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                Grid.SetRow(field.label, row);
                _cutResultGrid.Children.Add(field.label);

                field.text = new TextBox();
                field.text.Text = "";
                field.text.IsReadOnly = true;
                field.text.Margin = new System.Windows.Thickness(5);
                field.text.Background = new SolidColorBrush(k_computedColor);
                Grid.SetRow(field.text, row);
                Grid.SetColumn(field.text, 1);
                _cutResultGrid.Children.Add(field.text);

                field.units = new Label();
                field.units.Content = property.units;
                Grid.SetRow(field.units, row);
                Grid.SetColumn(field.units, 2);
                _cutResultGrid.Children.Add(field.units);

                field.model = property;

                row++;
            }

            _cutDB = ReadFromJsonFile<CutDB>("cutsdb.json");
            if (_cutDB == null)
                NewDB();
            else
            {
                _cutDB.AfterLoad();
                PushCutDBToTree();
            }

            _refreshing = false;
        }

        public void CutNameChanged()
        {
            if (_selectedCut != null && !_refreshing)
            {
                // push it to the model
                _selectedCut.Name = _cutNameText.Text;

                // update the tree
                if (_selectedItem != null)
                {
                    _selectedItem.Header = _selectedCut.Name;
                }
            }
        }

        private void FieldTextChanged(string name, InputProperty field)
        {
            if (_selectedCut != null && field.text.Text != "" && !_refreshing)
            {
                try
                {
                    // push input to cut
                    _selectedCut.SetProperty(name, Convert.ToDouble(field.text.Text));

                    // refresh computed values
                    RefreshComputedValues();
                }
                catch
                {

                }
            }
        }

        private void FieldTextChecked(string name, InputProperty field)
        {
            if (_selectedCut != null && field.text.Text != "" && !_refreshing)
            {
                // push input to cut
                _selectedCut.EnableAndSetProperty(name, Convert.ToDouble(field.text.Text));

                // refresh
                RefreshCut();
            }
        }

        private void FieldTextUnChecked(string name, InputProperty field)
        {
            if (_selectedCut != null && !_refreshing)
            {
                // push input to cut
                _selectedCut.DisableProperty(name);

                // refresh
                RefreshCut();
            }
        }

        void PushCutToTree(Cut cut, ItemCollection treeItems)
        {
            TreeViewItem item = new TreeViewItem();
            item.Header = cut.Name;
            item.Name = cut._guid;
            item.IsExpanded = true;
            item.AllowDrop = true;
            treeItems.Add(item);

            foreach (Cut child in cut.Children)
            {
                PushCutToTree(child, item.Items);
            }
        }

        void PushCutDBToTree()
        {
            _cutTree.Items.Clear();

            foreach (Cut cut in _cutDB.Cuts)
            {
                PushCutToTree(cut, _cutTree.Items);
            }
        }

        public void NewDB()
        {
            _cutDB = new CutDB();
            //_cutDB.Cuts.Add(new Cut());
            PushCutDBToTree();
        }

        public void NewCut()
        {
            if (_selectedCut != null)
            {
                _selectedCut.AddChild(new Cut());
            }
            else
            {
                _cutDB.Cuts.Add(new Cut());
            }

            PushCutDBToTree();

            Save();
        }

        public void DeleteCut()
        {
            if (_selectedCut != null)
            {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    _cutDB.Delete(_selectedCut);

                    _selectedCut = null;

                    _refreshing = true;
                    PushCutDBToTree();
                    ClearFields();
                    _refreshing = false;

                    Save();
                }
            }
        }

        public void SelectCut(string cutGuid, TreeViewItem selectedItem)
        {
            _selectedCut = _cutDB.GetCutByGuid(cutGuid);
            _selectedItem = selectedItem;
            RefreshCut();
        }

        public void DeselectCut()
        {
            _selectedCut = null;
            _selectedItem = null;

            _refreshing = true;
            ClearFields();
            _refreshing = false;
        }

        void ClearFields()
        {
            _cutNameText.Text = "";
            _cutNameText.IsReadOnly = true;

            foreach (KeyValuePair<string,InputProperty> field in _inputFields)
            {
                field.Value.text.Text = "";
                field.Value.text.Background = new SolidColorBrush(k_disabledColor);
                field.Value.text.IsReadOnly = true;
                field.Value.checkbox.IsChecked = false;
            }

            foreach (KeyValuePair<string, ComputedProperty> field in _computedFields)
            {
                field.Value.text.Text = "";
            }
        }

        void RefreshCut()
        {
            _refreshing = true;

            ClearFields();

            if (_selectedCut != null)
            {
                _cutNameText.Text = _selectedCut.Name;
                _cutNameText.IsReadOnly = false;

                foreach (KeyValuePair<string, InputProperty> field in _inputFields)
                {
                    double value = _selectedCut.GetProperty(field.Key);
                    bool enabled = _selectedCut.PropertyEnabled(field.Key);
                    field.Value.text.Text = value.ToString("0.######");
                    field.Value.text.Background = new SolidColorBrush(enabled ? k_enabledColor : k_disabledColor);
                    field.Value.text.IsReadOnly = !enabled;
                    field.Value.checkbox.IsChecked = enabled;
                }

                RefreshComputedValues();
            }

            _refreshing = false;
        }

        void RefreshComputedValues()
        {
            if (_selectedCut != null)
            {
                foreach (KeyValuePair<string, ComputedProperty> field in _computedFields)
                {
                    double value = _selectedCut.ComputeValue(field.Key);
                    field.Value.text.Text = value.ToString("0.#####");

                    bool warn = field.Value.model.warnAbove != 0 && value > field.Value.model.warnAbove;
                    warn = warn || (field.Value.model.warnBelow != 0 && value < field.Value.model.warnBelow);
                    field.Value.text.Background = new SolidColorBrush(warn ? k_warningColor : k_computedColor);
                }

                var x = Enumerable.Range(0, 360).Select(i => (double)i).ToList();
                var y = _selectedCut.ComputeCuttingForces(x);
                double total = y.Sum();
                //_graph.Description = "Force " + total;
                _graph.Plot(x, y);
                DataRect rect = _graph.ActualPlotRect;
                _graph.SetPlotRect(new DataRect(0, 0, 360, y.Max()));
            }

            Save();
        }

        public void Save()
        {
            WriteToJsonFile("cutsdb.json", _cutDB);
        }

        public static void WriteToXmlFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
        {
            TextWriter writer = null;
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                writer = new StreamWriter(filePath, append);
                serializer.Serialize(writer, objectToWrite);
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.Message);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        public static void WriteToJsonFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
        {
            TextWriter writer = null;
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                using (var stream = File.Create(filePath))
                    serializer.WriteObject(stream, objectToWrite);
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.Message);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        public static T ReadFromXmlFile<T>(string filePath) where T : new()
        {
            TextReader reader = null;
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                reader = new StreamReader(filePath);
                return (T)serializer.Deserialize(reader);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }

        public static T ReadFromJsonFile<T>(string filePath) where T : new()
        {
            T result = default(T);
            TextReader reader = null;
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                using (var stream = File.OpenRead(filePath))
                    result = (T)serializer.ReadObject(stream);
            }
            catch (Exception)
            {
                result = default(T);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }

            return result;
        }

        public void DragCut(string sourceGuid, string destGuid)
        {
            if (sourceGuid != destGuid)
            {
                Cut source = _cutDB.GetCutByGuid(sourceGuid);
                Cut dest = _cutDB.GetCutByGuid(destGuid);
                if (source != null && dest != null)
                {
                    if (_cutDB.Reparent(source, dest))
                    {
                        _refreshing = true;
                        PushCutDBToTree();
                        ClearFields();
                        _refreshing = false;

                        Save();
                    }
                }
            }
        }
    }
}
