using System;
using System.Windows;
using System.Windows.Controls;

namespace ZoidsBattle
{
    public partial class MovementSpeedDialog : Window
    {
        public MovementType SelectedMovementType { get; private set; }
        public double SelectedSpeed { get; private set; }
        public double SelectedAngleChange { get; private set; }
        
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
            UpdateCircleControlsVisibility();
        }

        private void MovementTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
            UpdateCircleControlsVisibility();
        }

        private void UpdateCircleControlsVisibility()
        {
            if (MovementTypeCombo.SelectedItem == null) return;
            
            var selectedItem = (ComboBoxItem)MovementTypeCombo.SelectedItem;
            var movementType = Enum.Parse<MovementType>(selectedItem.Tag.ToString() ?? "StandStill");
            
            bool isCircle = (movementType == MovementType.Circle);
            CircleControlsGroup.Visibility = isCircle ? Visibility.Visible : Visibility.Collapsed;
            
            // Update angle slider maximum based on Zoid capabilities
            if (isCircle && _currentZoid != null)
            {
                double maxAngle = CalculateMaxCircleAngle(_currentZoid, _currentDistance);
                AngleSlider.Maximum = maxAngle;
                
                // Adjust current value if it exceeds the new maximum
                if (AngleSlider.Value > maxAngle)
                {
                    AngleSlider.Value = maxAngle;
                }
            }
        }

        private double CalculateMaxCircleAngle(Zoid zoid, double distance)
        {
            // Get the Zoid's speed for the current terrain
            double speed = zoid.GetSpeed("land"); // Default to land for now
            
            // Calculate the circumference at the current distance
            // Using simplified geometry: circumference = 2 * π * distance
            double circumference = 2 * Math.PI * distance;
            
            // Calculate what fraction of the circumference the Zoid can travel
            double fractionOfCircle = speed / circumference;
            
            // Convert to degrees (360° = full circle)
            double maxAngleDegrees = fractionOfCircle * 360.0;
            
            // Cap at reasonable limits:
            // - Minimum: 15° (always allow some movement)
            // - Maximum: 180° (can't circle more than halfway around)
            maxAngleDegrees = Math.Max(15.0, Math.Min(180.0, maxAngleDegrees));
            
            return maxAngleDegrees;
        }

        private void AngleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (AngleValueText != null)
            {
                AngleValueText.Text = $"{e.NewValue:F0}°";
            }
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
            
            // Capture angle change for circle movement
            if (SelectedMovementType == MovementType.Circle)
            {
                var directionItem = (ComboBoxItem)CircleDirectionCombo.SelectedItem;
                string direction = directionItem?.Tag?.ToString() ?? "Clockwise";
                double angle = AngleSlider.Value;
                
                // Clockwise is positive, Counter-clockwise is negative
                SelectedAngleChange = direction == "Clockwise" ? angle : -angle;
            }
            else
            {
                SelectedAngleChange = 0;
            }
            
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
