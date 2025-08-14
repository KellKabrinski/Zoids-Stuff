using System;
using System.Windows;
using System.Windows.Controls;

namespace ZoidsBattle
{
    public partial class MovementSpeedDialog : Window
    {
        public MovementType SelectedMovementType { get; private set; }
        public double SelectedSpeed { get; private set; }
        
        private readonly double _maxSpeed;
        private readonly double _currentDistance;
        private readonly Zoid? _currentZoid;

        public MovementSpeedDialog(double maxSpeed, MovementType initialMovementType, double currentDistance = 1000, Zoid? currentZoid = null)
        {
            InitializeComponent();
            
            _maxSpeed = maxSpeed;
            _currentDistance = currentDistance;
            _currentZoid = currentZoid;
            
            MaxSpeedText.Text = maxSpeed.ToString("F0");
            SpeedSlider.Maximum = maxSpeed;
            CurrentDistanceText.Text = $"Current Distance: {currentDistance:F0}m";
            
            // Set initial movement type
            foreach (ComboBoxItem item in MovementTypeCombo.Items)
            {
                if (item.Tag.ToString() == initialMovementType.ToString())
                {
                    MovementTypeCombo.SelectedItem = item;
                    break;
                }
            }
            
            if (MovementTypeCombo.SelectedItem == null && MovementTypeCombo.Items.Count > 0)
            {
                MovementTypeCombo.SelectedIndex = 0;
            }
            
            UpdatePreview();
        }

        private void MovementTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SpeedValueText != null)
            {
                SpeedValueText.Text = e.NewValue.ToString("F0");
                UpdatePreview();
            }
        }

        private void UpdatePreview()
        {
            if (MovementTypeCombo.SelectedItem == null) return;
            
            var selectedItem = (ComboBoxItem)MovementTypeCombo.SelectedItem;
            var movementType = Enum.Parse<MovementType>(selectedItem.Tag?.ToString() ?? "StandStill");
            double speed = SpeedSlider.Value;
            
            double projectedDistance = CalculateProjectedDistance(_currentDistance, movementType, speed);
            ProjectedDistanceText.Text = $"After Movement: {projectedDistance:F0}m";
            
            // Update attack range preview if we have a zoid
            if (_currentZoid != null)
            {
                string rangeText = GetAttackRangeText(projectedDistance);
                AttackRangePreviewText.Text = $"Attack Range: {rangeText}";
            }
            else
            {
                AttackRangePreviewText.Text = "Attack Range: N/A";
            }
        }

        private double CalculateProjectedDistance(double currentDistance, MovementType movementType, double moveDistance)
        {
            switch (movementType)
            {
                case MovementType.Close:
                    return Math.Max(0, currentDistance - moveDistance);
                case MovementType.Retreat:
                    return currentDistance + moveDistance;
                case MovementType.Circle:
                case MovementType.Search:
                    return currentDistance; // Distance doesn't change for these
                case MovementType.StandStill:
                default:
                    return currentDistance;
            }
        }

        private string GetAttackRangeText(double distance)
        {
            if (distance <= 200)
                return "Melee";
            else if (distance <= 500)
                return "Close";
            else if (distance <= 1000)
                return "Mid";
            else
                return "Long";
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (MovementTypeCombo.SelectedItem == null)
            {
                MessageBox.Show("Please select a movement type.", "Selection Required", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var selectedItem = (ComboBoxItem)MovementTypeCombo.SelectedItem;
            SelectedMovementType = Enum.Parse<MovementType>(selectedItem.Tag?.ToString() ?? "StandStill");
            SelectedSpeed = SpeedSlider.Value;
            
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
