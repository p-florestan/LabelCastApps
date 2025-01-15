using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.Windows.Forms;
using System.ComponentModel;

namespace LabelCastDesktop
{

    public class DictionaryEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider? provider, object? value)
        {
            if (value is Dictionary<string, string> dictionary)
            {
                using (var editorForm = new DictionaryEditorForm(dictionary))
                {
                    if (editorForm.ShowDialog() == DialogResult.OK)
                    {
                        return editorForm.EditedDictionary;
                    }
                }
            }
            return value;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context)
        {
            return UITypeEditorEditStyle.Modal;
        }
    }

}
