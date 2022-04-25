﻿using AutocompleteMenuNS;
using Newtonsoft.Json.Linq;
using ScintillaNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using V2RayG.Resources.Resx;

namespace V2RayG.Controllers.ConfigerComponet
{
    class Editor : ConfigerComponentController
    {
        Services.Cache cache;

        Scintilla editor = null;
        AutocompleteMenu jsonAcm = null;
        ComboBox cboxSection;
        Button btnFormat, btnRestore;

        Dictionary<string, string> sections;
        string preSection = @"";
        string ConfigDotJson = Apis.Models.Consts.Config.ConfigDotJson;

        public Editor(
            Panel panel,
            ComboBox cboxSection,
            Button btnFormat,
            Button btnRestore)
        {
            cache = Services.Cache.Instance;

            this.cboxSection = cboxSection;
            this.btnFormat = btnFormat;
            this.btnRestore = btnRestore;

            CreateEditor(panel);
        }

        #region properties
        private string _content;

        public string content
        {
            get
            {
                return _content;
            }
            set
            {
                SetField(ref _content, value);
            }
        }
        #endregion

        #region pulbic method
        public void Prepare()
        {
            preSection = ConfigDotJson;
            RefreshSections();
            cboxSection.Text = preSection;
            AttachEditorEvents();
            jsonAcm = cache.GetJsonAcm()?.BindToEditor(editor);
            ShowSection();
            AttachControlEvents();
        }

        public void Cleanup()
        {
            jsonAcm.SetAutocompleteMenu(editor, null);
            jsonAcm.Dispose();
            jsonAcm = null;
        }

        public void DiscardChanges()
        {
            var config = container.config;

            if (preSection == ConfigDotJson)
            {
                content = config.ToString();
                return;
            }

            var part = Misc.Utils.GetKey(config, preSection);
            if (part != null)
            {
                content = part.ToString();
                return;
            }

            content = sections[preSection];
        }

        public bool IsChanged()
        {
            if (!CheckValid())
            {
                return true;
            }

            var content = JToken.Parse(this.content);
            var section = GetCurConfigSection();

            if (JToken.DeepEquals(content, section))
            {
                return false;
            }

            return true;
        }

        public Scintilla GetEditor()
        {
            if (editor == null)
            {
                throw new ArgumentNullException("Editor not ready!");
            }
            return editor;
        }

        public void ReloadSection()
        {
            RefreshSections();
            ShowSection();
        }

        public void ShowSection()
        {
            var key = preSection;
            var config = container.config;

            if (!sections.Keys.Contains(key))
            {
                key = ConfigDotJson;
                preSection = key;
            }

            if (key == ConfigDotJson)
            {
                content = config.ToString();
                return;
            }

            var part = Misc.Utils.GetKey(config, key);
            if (part != null)
            {
                content = part.ToString();
                return;
            }

            var c = sections[key];
            var token = Misc.Utils.CreateJObject(key, JToken.Parse(c));
            Misc.Utils.MergeJson(config, token);
            content = c;
            RefreshSections();
        }

        public void ShowEntireConfig()
        {
            this.cboxSection.Text = ConfigDotJson;
        }

        public bool Flush()
        {
            if (!CheckValid())
            {
                if (Misc.UI.Confirm(I18N.EditorDiscardChange))
                {
                    DiscardChanges();
                }
                else
                {
                    return false;
                }
            }

            SaveChanges();
            container.InvokeOnChanged();

            return true;
        }



        public override void Update(JObject config)
        {
            // do nothing
        }
        #endregion

        #region Scintilla
        private int maxLineNumberCharLength;
        private void Scintilla_TextChanged(object sender, EventArgs e)
        {
            // Did the number of characters in the line number display change?
            // i.e. nnn VS nn, or nnnn VS nn, etc...
            var maxLineNumberCharLength = editor.Lines.Count.ToString().Length;
            if (maxLineNumberCharLength == this.maxLineNumberCharLength)
                return;

            // Calculate the width required to display the last line number
            // and include some padding for good measure.
            const int padding = 2;
            editor.Margins[0].Width = editor.TextWidth(Style.LineNumber, new string('9', maxLineNumberCharLength + 1)) + padding;
            this.maxLineNumberCharLength = maxLineNumberCharLength;
        }

        string GetCurrentLineText(int endPos)
        {
            int curPos = editor.CurrentPosition;
            int lineNumber = editor.LineFromPosition(curPos);
            int startPos = editor.Lines[lineNumber].Position;
            return editor.GetTextRange(startPos, (endPos - startPos)); //Text until the caret so that the whitespace is always equal in every line.
        }

        private void Scintilla_InsertCheck(object sender, InsertCheckEventArgs e)
        {
            if ((e.Text.EndsWith("\n") || e.Text.EndsWith("\r")))
            {
                //Text until the caret so that the whitespace is always equal in every line.
                string curLineText = GetCurrentLineText(e.Position);
                Match curIndentMatch = Regex.Match(curLineText, "^[ \\t]*");
                string curIndent = curIndentMatch.Value;

                e.Text = (e.Text + curIndent);

                if (Regex.IsMatch(curLineText, @"\[\s*$")
                    || Regex.IsMatch(curLineText, @"{\s*$"))
                {
                    e.Text = (e.Text + "  ");
                }
            }
        }

        private void Scintilla_CharAdded(object sender, CharAddedEventArgs e)
        {
            int curLine = editor.LineFromPosition(editor.CurrentPosition);
            if (curLine < 2)
            {
                return;
            }

            string ct = editor.Lines[curLine].Text.Trim();
            if (ct == "}" || ct == "]")
            { //Check whether the bracket is the only thing on the line.. For cases like "if() { }".
                SetIndent(editor, curLine, GetIndent(editor, curLine - 1) - 2);
            }
        }

        //Codes for the handling the Indention of the lines.
        //They are manually added here until they get officially added to the Scintilla control.

        const int SCI_SETLINEINDENTATION = 2126;
        const int SCI_GETLINEINDENTATION = 2127;
        private void SetIndent(Scintilla scin, int line, int indent)
        {
            scin.DirectMessage(SCI_SETLINEINDENTATION, new IntPtr(line), new IntPtr(indent));
        }
        private int GetIndent(Scintilla scin, int line)
        {
            return (scin.DirectMessage(SCI_GETLINEINDENTATION, new IntPtr(line), (IntPtr)null).ToInt32());
        }
        #endregion

        #region private method
        void AttachEditorEvents()
        {
            editor.InsertCheck += Scintilla_InsertCheck;
            editor.CharAdded += Scintilla_CharAdded;
            editor.TextChanged += Scintilla_TextChanged;
        }

        bool IsJsonCollection(JToken token)
        {
            if (token == null)
            {
                return false;
            }

            if (token.Type == JTokenType.Object
                  || token.Type == JTokenType.Array)
            {
                return true;
            }

            return false;
        }

        Dictionary<string, string> GetValidSections()
        {
            var config = container.config;
            var defSections = Apis.Models.Consts.Config.GetDefCfgSections();

            Apis.Misc.Utils
                .GetterJsonSections(config)
                .Where(kv => IsJsonCollection(Misc.Utils.GetKey(config, kv.Key)))
                .ToList()
                .ForEach(kv => defSections[kv.Key] = kv.Value);

            return defSections;
        }

        void RefreshSections()
        {
            sections = GetValidSections();
            RefreshCboxSectionsItems();
        }

        void RefreshCboxSectionsItems()
        {
            var oldText = cboxSection.Text;
            var keys = sections.Keys.ToList();
            keys.Sort((a, b) => Apis.Misc.Utils.JsonKeyComparer(a, b));
            keys.Insert(0, ConfigDotJson);

            cboxSection.Items.Clear();
            cboxSection.Items.AddRange(keys.ToArray());
            Misc.UI.ResetComboBoxDropdownMenuWidth(cboxSection);
            cboxSection.Text = oldText;
        }

        void OnCboxSectionTextChangedHandler(object sender, EventArgs args)
        {
            cboxSection.TextChanged -= OnCboxSectionTextChangedHandler;

            CboxSectionTextChangedWorker();

            cboxSection.TextChanged += OnCboxSectionTextChangedHandler;
        }

        void CboxSectionTextChangedWorker()
        {
            var text = cboxSection.Text;
            if (text == preSection)
            {
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                cboxSection.Text = preSection;
                return;
            }

            if (!IsReadyToSwitchSection())
            {
                cboxSection.Text = preSection;
                return;
            }

            RefreshSections();

            preSection = text;
            ShowSection();

            // show section may change preSection;
            cboxSection.Text = preSection;

            container.Update();
        }

        void AttachControlEvents()
        {
            cboxSection.TextChanged += OnCboxSectionTextChangedHandler;

            btnFormat.Click += (s, e) =>
            {
                FormatCurrentContent();
            };

            btnRestore.Click += (s, e) =>
            {
                DiscardChanges();
            };
        }

        bool IsReadyToSwitchSection()
        {
            if (CheckValid())
            {
                SaveChanges();
                container.InvokeOnChanged();
                return true;
            }

            return Misc.UI.Confirm(I18N.CannotParseJson);
        }

        void FormatCurrentContent()
        {
            try
            {
                var json = JToken.Parse(content);
                content = json.ToString();
            }
            catch
            {
                MessageBox.Show(I18N.PleaseCheckConfig);
            }
        }

        JToken GetCurConfigSection()
        {
            var config = container.config;

            if (preSection == ConfigDotJson)
            {
                return config.DeepClone();
            }

            var part = Misc.Utils.GetKey(config, preSection);
            if (part != null)
            {
                return part.DeepClone();
            }
            return JToken.Parse(sections[preSection]);
        }

        void SaveChanges()
        {
            var content = JToken.Parse(this.content);

            if (preSection == ConfigDotJson)
            {
                container.config = content as JObject;
                RefreshSections();
                return;
            }

            var config = container.config;

            var part = Misc.Utils.GetKey(config, preSection);
            if (part != null)
            {
                part.Replace(content);
            }
            else
            {
                var mixin = Misc.Utils.CreateJObject(preSection, content);
                Misc.Utils.MergeJson(config, mixin);
            }
            RefreshSections();
        }

        bool CheckValid()
        {
            try
            {
                JToken.Parse(content);
                return true;
            }
            catch
            {
                return false;
            }
        }

        void CreateEditor(Panel container)
        {
            var editor = Misc.UI.CreateScintilla(container);

            Apis.Misc.Utils.BindEditorDragDropEvent(editor);

            this.editor = editor;

            // bind scintilla
            var bs = new BindingSource();
            bs.DataSource = this;
            editor.DataBindings.Add(
                "Text",
                bs,
                nameof(this.content),
                true,
                DataSourceUpdateMode.OnPropertyChanged);
        }

        #endregion
    }
}
