﻿using System.Windows;

namespace DsmSuite.DsmViewer.View.Dialogs
{
    /// <summary>
    /// Interaction logic for ElementEditAnnotationDialog.xaml
    /// </summary>
    public partial class ElementEditAnnotationDialog
    {
        public ElementEditAnnotationDialog()
        {
            InitializeComponent();
        }

        private void OnOkButtonClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
