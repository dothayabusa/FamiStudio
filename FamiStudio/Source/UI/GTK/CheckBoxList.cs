﻿using Gdk;
using Gtk;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class CheckBoxList : VBox
    {
        public delegate void CheckChangedDelegate(CheckBoxList sender, int idx, bool value);
        public event CheckChangedDelegate CheckChangedEvent;

        VBox vbox;
        List<CheckButton> checkButtons = new List<CheckButton>();

        public CheckBoxList(string[] values, bool[] selected, int height)
        {
            vbox = new VBox();
            vbox.Show();

            CreateCheckboxes(values, selected);

            var scroll = new ScrolledWindow();
            scroll.SetPolicy(PolicyType.Never, PolicyType.Automatic);
            scroll.AddWithViewport(vbox);
            scroll.Show();
            scroll.Child.HeightRequest = GtkUtils.ScaleGtkWidget(200);

            Add(scroll);
        }

        private void RemoveAllCheckboxes()
        {
            foreach (var chk in checkButtons)
                vbox.Remove(chk);
            checkButtons.Clear();
        }

        private void CreateCheckboxes(string[] values, bool[] selected)
        {
            for (int i = 0; i < values.Length; i++)
            {
                var chk = new CheckButton(values[i]);
                chk.Active = selected == null ? true : selected[i];
                chk.Name = "CheckBoxList";
                chk.CanFocus = false;
                chk.Toggled += Chk_Toggled;
                chk.Show();
                vbox.PackStart(chk, false, false, 0);
                checkButtons.Add(chk);
            }
        }

        void Chk_Toggled(object sender, EventArgs e)
        {
            var chk = sender as CheckButton;
            var idx = checkButtons.IndexOf(chk);
            CheckChangedEvent?.Invoke(this, idx, chk.Active);
        }

        public void Update(string[] values, bool[] selected)
        {
            RemoveAllCheckboxes();
            CreateCheckboxes(values, selected);
        }

        public void Update(bool[] selected)
        {
            Debug.Assert(checkButtons.Count == selected.Length);

            for (int i = 0; i < checkButtons.Count; i++)
                checkButtons[i].Active = selected[i];
        }

        public bool[] GetSelected()
        {
            var selected = new bool[checkButtons.Count];

            for (int i = 0; i < checkButtons.Count; i++)
            {
                selected[i] = checkButtons[i].Active;
            }

            return selected;
        }
    }
}
