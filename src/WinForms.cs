using System;
using System.Linq;
using System.Windows.Forms;

namespace HallLibrary.Extensions
{
    class TextBoxWithDoubleClickSupport : TextBox
	{
		const int WM_LBUTTONDBLCLK = 0x0203;
		char[] WordSeparators = new [] {
			'.',
			'/',
			'\\',
			'(',
			')',
			'"',
			'\'',
			'-',
			':',
			',',
			';',
			'<',
			'>',
			'~',
			'!',
			'@',
			'#',
			'$',
			'%',
			'^',
			'&',
			'*',
			'|',
			'+',
			'=',
			'[',
			']',
			'{',
			'}',
			'`',
			'~',
			'?',
			' ',
			'\t',
			'\n',
			'\r'
		};
		
		protected override void WndProc(ref System.Windows.Forms.Message m)
		{
			if (m.Msg == WM_LBUTTONDBLCLK)
			{
				var word_begin = this.SelectionStart;
				if (word_begin < this.TextLength)
					word_begin = this.Text.LastIndexOfAny(WordSeparators, word_begin);
				else
					word_begin --;
				
				var word_end = this.Text.IndexOfAny(WordSeparators, this.SelectionStart);
				if (word_end == -1) word_end = this.TextLength;
				if (word_begin == word_end)
				{
					if (char.IsWhiteSpace(this.Text[word_begin])) // the character double clicked on is whitespace
					{
						// find first preceding whitespace char (stop at the beginning of the line)
						while (word_begin > 0 && char.IsWhiteSpace(this.Text[word_begin]) && this.Text[word_begin] != '\n')
							word_begin--;
						// find last consecutive whitespace char (stop at the end of the line)
						while (word_end < this.TextLength && char.IsWhiteSpace(this.Text[word_end]) && this.Text[word_end] != '\n')
							word_end++;
					}
					else
					{
						// find first preceding consecutive word boundary char (stop at the beginning of the line or at whitespace)
						while (word_begin > 0 && WordSeparators.Contains(this.Text[word_begin]) && !char.IsWhiteSpace(this.Text[word_begin]))
							word_begin--;
						// find last consecutive word boundary char (stop at whitespace)
						while (word_end < this.TextLength && WordSeparators.Contains(this.Text[word_end]) && !char.IsWhiteSpace(this.Text[word_end]))
							word_end++;
					}
				}
				this.SelectionStart = word_begin + 1;
				this.SelectionLength = word_end - word_begin - 1;
				return;
			}
            
			base.WndProc(ref m);
		}
	}
	
	/*
	class DraggableForm : Form
	{
		protected override void WndProc(ref Message m)
		{
			const UInt32 WM_NCHITTEST = 0x0084;
			const UInt32 HTCAPTION = 0x2;
			
			bool handled = false;
			if (m.Msg == WM_NCHITTEST)
			{
				if (this.PointToClient(Cursor.Position).X > 50) // example - only allow dragging if the X (client) co-ordinate is greater than 50
				{
					m.Result = (IntPtr)HTCAPTION;
					handled = true;
				}
			}
			
			if (!handled)
				base.WndProc(ref m);
		}
	}
	*/
}
