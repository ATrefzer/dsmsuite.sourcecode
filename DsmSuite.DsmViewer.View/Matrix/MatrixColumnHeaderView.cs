﻿using System;
using System.Windows;
using DsmSuite.DsmViewer.ViewModel.Matrix;
using System.Windows.Input;
using System.Windows.Media;

namespace DsmSuite.DsmViewer.View.Matrix
{
    public class MatrixColumnHeaderView : MatrixFrameworkElement
    {
        private MatrixViewModel _viewModel;
        private readonly RenderTheme _renderTheme;
        private Rect _rect;
        private int? _hoveredColumn;
        private double _pitch;
        private double _offset;

        public MatrixColumnHeaderView()
        {
            _renderTheme = new RenderTheme(this);
            _rect = new Rect(new Size(_renderTheme.MatrixCellSize, _renderTheme.MatrixHeaderHeight));
            _hoveredColumn = null;
            _pitch = _renderTheme.MatrixCellSize + 2.0;
            _offset = 1.0;

            DataContextChanged += OnDataContextChanged;
            MouseMove += OnMouseMove;
            MouseDown += OnMouseDown;
            MouseLeave += OnMouseLeave;
        }

        private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            _viewModel = DataContext as MatrixViewModel;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnPropertyChanged;
                InvalidateVisual();
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            int column = GetHoveredColumn(e.GetPosition(this));
            if (_hoveredColumn != column)
            {
                _hoveredColumn = column;
                _viewModel.HoverColumn(column);
            }
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _viewModel.HoverColumn(null);
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            int column = GetHoveredColumn(e.GetPosition(this));
            _viewModel.SelectColumn(column);
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == nameof(MatrixViewModel.MatrixSize)) ||
                (e.PropertyName == nameof(MatrixViewModel.HoveredColumn)) ||
                (e.PropertyName == nameof(MatrixViewModel.SelectedColumn)))
            {
                ToolTip = _viewModel.ColumnTooltip;
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (_viewModel != null)
            {
                int matrixSize = _viewModel.MatrixSize;
                for (int column = 0; column < matrixSize; column++)
                {
                    _rect.X = _offset + column * _pitch;
                    _rect.Y = 0;

                    bool isHovered = _viewModel.HoveredColumn.HasValue && (column == _viewModel.HoveredColumn.Value);
                    bool isSelected = _viewModel.SelectedColumn.HasValue && (column == _viewModel.SelectedColumn.Value);
                    MatrixColor color = _viewModel.ColumnColors[column];
                    SolidColorBrush background = _renderTheme.GetBackground(color, isHovered, isSelected);

                    dc.DrawRectangle(background, null, _rect);

                    int id = _viewModel.ColumnElementIds[column];
                    Point location = new Point(_rect.X + 10.0, _rect.Y - 5.0);
                    DrawRotatedText(dc, location, id.ToString(), _rect.Width - 2.0);
                }

                Height = _renderTheme.MatrixHeaderHeight + 2.0;
                Width = _renderTheme.MatrixCellSize * matrixSize + 2.0;
            }
        }
        
        private int GetHoveredColumn(Point location)
        {
            double column = (location.X - _offset) / _pitch;
            return (int)column;
        }
    }

}