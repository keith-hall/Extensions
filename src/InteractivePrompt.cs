using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;

namespace HallLibrary.Extensions
{
	/// <summary>
	/// A class used to build a simple input form at runtime, with optional validation.
	/// Can cache inputs to current app domain, useful for when running code in LINQPad.
	/// </summary>
	public class InteractivePrompt
	{
		private TableLayoutPanel layout = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, Padding = new Padding(3) };
		private ErrorProvider showErrors = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };
		
		public Control AddCached<T> (string label, T defaultValue, Action<T> saved, Func<T, bool> validate = null, string invalidMessage = "Invalid value")
		{
			var cached = AppDomain.CurrentDomain.GetData(label);
			
			var value = (cached != null) ? (T)cached : defaultValue;
			
			return Add(label, value, newValue => { AppDomain.CurrentDomain.SetData(label, newValue); saved(newValue); }, validate, invalidMessage);
		}
		
		public Control Add<T> (string label, T value, Action<T> save, Func<T, bool> validate = null, string invalidMessage = "Invalid value")
		{
			var row = ++layout.RowCount;
			var ctrl = ControlFactory.CreateControl(value);
			var lbl = ControlFactory.CreateLabel(label);
			lbl.Click += (o, e) => ctrl.Focus();
			layout.Controls.Add(lbl, 0, row);
			layout.Controls.Add(ctrl, 1, row);
			
			showErrors.SetIconPadding(ctrl, 3);
			ctrl.Margin = new Padding(0, 0, showErrors.Icon.Width + showErrors.GetIconPadding(ctrl) * 2, 0);
			ctrl.Dock = DockStyle.Fill;
			
			Func<object, T> getValue = obj => (T)Convert.ChangeType(obj, typeof(T));
			if (ctrl.GetType() == typeof(ComboBox))
				getValue = obj => (T)Enum.Parse(typeof(T), obj.ToString());
			Action<object, bool> validateAndSave = (newValue, saveValue) => {
				var isValid = validate == null || validate(getValue(newValue));
				
				showErrors.SetError(ctrl, isValid ? string.Empty : invalidMessage);
				
				if (saveValue)
					save(getValue(newValue));
			};
			
			Action<bool> valueUpdated = (saveValue) => {
				if (ctrl.GetType() == typeof(TextBox))
					validateAndSave(((TextBox)ctrl).Text, saveValue);
				else if (ctrl.GetType() == typeof(NumericUpDown))
					validateAndSave(((NumericUpDown)ctrl).Value, saveValue);
				else if (ctrl.GetType() == typeof(CheckBox))
					validateAndSave(((CheckBox)ctrl).Checked, saveValue);
				else if (ctrl.GetType() == typeof(ComboBox))
					validateAndSave(((ComboBox)ctrl).SelectedItem, saveValue);
				else if (ctrl.GetType() == typeof(DateTimePicker))
					validateAndSave(((DateTimePicker)ctrl).Value, saveValue);
			};
			
			if (validate != null)
			{
				Action<object, EventArgs> checkEvent = (o, e) => valueUpdated(false);
				EventHandler handler = checkEvent.Invoke;
				ctrl.LostFocus += handler;
				
				if (ctrl.GetType() == typeof(TextBox))
					((TextBox)ctrl).TextChanged += handler;
				else if (ctrl.GetType() == typeof(NumericUpDown))
					((NumericUpDown)ctrl).ValueChanged += handler;
				else if (ctrl.GetType() == typeof(CheckBox))
					((CheckBox)ctrl).CheckedChanged += handler;
				else if (ctrl.GetType() == typeof(ComboBox))
					((ComboBox)ctrl).SelectedIndexChanged += handler;
				else if (ctrl.GetType() == typeof(DateTimePicker))
					((DateTimePicker)ctrl).ValueChanged += handler;
				
				valueUpdated(false);
			}
			ctrl.Tag = valueUpdated;
			return ctrl;
		}
		
		private bool IsValid ()
		{
			return (GetControls().All(c => showErrors.GetError(c) == string.Empty));
		}
		
		private IEnumerable<Control> GetControls ()
		{
			return Enumerable.Range(1, layout.RowCount).Select(r => layout.GetControlFromPosition(1, r));
		}
		
		public DialogResult Prompt ()
		{
			using (var f = new Form { StartPosition = FormStartPosition.CenterParent }) {
				layout.Controls.Add(new Label(), 1, layout.RowCount + 1); // prevent last control being aligned incorrectly
				layout.AutoScroll = true;
				
				var sc = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
				sc.Panel1.Controls.Add(layout);
				
				var ok = new Button { Text = "OK", DialogResult = DialogResult.OK };
				f.AcceptButton = ok;
				ok.Click += (o, e) => f.DialogResult = IsValid() ? DialogResult.OK : DialogResult.None; // prevent form from closing if not valid
				
				var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
				f.CancelButton = cancel;
				
				var fl = new FlowLayoutPanel { Dock = DockStyle.Right, WrapContents = false };
				fl.Controls.Add(ok);
				fl.Controls.Add(cancel);
				fl.Width = ok.ClientSize.Width + cancel.ClientSize.Width + 20;
				sc.FixedPanel = FixedPanel.Panel2;
				sc.Panel2.Controls.Add(fl);
				
				f.ClientSize = new Size(f.ClientSize.Width, layout.GetRowHeights().Sum() + sc.Panel2.Height + 5);
				
				sc.IsSplitterFixed = true;
				f.Controls.Add(sc);
				
				var result = f.ShowDialog();
				if (result == DialogResult.OK)
					foreach (var ctrl in GetControls())
						(ctrl.Tag as Action<bool>).Invoke(true);
				
				return result;
			}
		}
	}
		
	public static class ControlFactory
	{
		public static Control CreateControl<T>(T item)
		{
			Control ctrl = null;
			// The control depends on the property type
			if (typeof(T) == typeof(string))
			{
				var textbox = new TextBox();
				textbox.Text = item.ToString();
				
				ctrl = textbox;
			}
			else if (typeof(T) == typeof(int) || typeof(T) == typeof(decimal) || typeof(T) == typeof(double))
			{
				var numeric = new NumericUpDown();
				numeric.Value = Convert.ToDecimal(item);
				numeric.DecimalPlaces = (typeof(T) == typeof(int)) ? 0 : 2;
				
				ctrl = numeric;
			}
			else if (typeof(T) == typeof(bool))
			{
				var checkbox = new CheckBox();
				checkbox.Checked = Convert.ToBoolean(item);
				
				ctrl = checkbox;
			}
			else if (typeof(T).BaseType == typeof(Enum))
			{
				var dropdown = new ComboBox();
				dropdown.DropDownStyle = ComboBoxStyle.DropDownList;
				dropdown.Items.AddRange(Enum.GetNames(typeof(T)));
				dropdown.SelectedItem = Convert.ToString(item);
				
				ctrl = dropdown;
			}
			else if (typeof(T) == typeof(DateTime))
			{
				var date = new DateTimePicker();
				date.Value = Convert.ToDateTime(item);
				if (date.Value.TimeOfDay.TotalMilliseconds > 0) {
					date.CustomFormat = GetUniversalDateFormat();
					date.Format = DateTimePickerFormat.Custom;
				}
				
				ctrl = date;
			} else
				throw new InvalidOperationException(string.Format("Unable to create control for type {0}", typeof(T).FullName));
			
			
			return ctrl;
		}
		
		public static string GetUniversalDateFormat ()
		{
			return System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.UniversalSortableDateTimePattern.Replace("'Z'", string.Empty);
		}
		
		/// <summary>
		/// Creates a new instance of the Label control using the specified text value.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static Label CreateLabel(string text)
		{
			var label = new Label();
			label.Text = GetLabel(text) + ":";
			label.AutoSize = true;
			label.Margin = new Padding(3, 6, 6, 0);
			return label;
		}
		
		/// <summary>
		/// Returns a friendly label from the supplied name. For example, the
		/// string "firstName" would be returned as "First Name".
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string GetLabel(string text)
		{
			if (text.Contains(" "))
				return text;
			return String.Join(string.Empty, text.AsEnumerable().Select(
				(c, i) => ((Char.IsUpper(c) || i == 0) ? " " + Char.ToUpper(c).ToString() : c.ToString())
			)).Trim();
		}
	}
}
