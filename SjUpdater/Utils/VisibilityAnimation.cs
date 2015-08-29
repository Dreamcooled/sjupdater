using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace SjUpdater.Utils
{
    //From http://stackoverflow.com/a/9195153
    public class VisibilityAnimation : DependencyObject
    {
        #region Private Variables

        private static HashSet<UIElement> HookedElements = new HashSet<UIElement>();
        private static DoubleAnimation FadeAnimation = new DoubleAnimation();
        private static bool SurpressEvent;
        private static bool Running;

        #endregion

        #region Attached Dependencies

        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.RegisterAttached("IsActive", typeof(bool), typeof(VisibilityAnimation), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsActivePropertyChanged)));
        public static bool GetIsActive(UIElement element)
        {
            if (element == null) throw new ArgumentNullException("element");
            return (bool)element.GetValue(IsActiveProperty);
        }
        public static void SetIsActive(UIElement element, bool value)
        {
            if (element == null) throw new ArgumentNullException("element");
            element.SetValue(IsActiveProperty, value);
        }

        public static readonly DependencyProperty FadeInDurationProperty = DependencyProperty.RegisterAttached("FadeInDuration", typeof(double), typeof(VisibilityAnimation), new PropertyMetadata(0.1));
        public static double GetFadeInDuration(UIElement e)
        {
            if (e == null) throw new ArgumentNullException("element");
            return (double)e.GetValue(FadeInDurationProperty);
        }
        public static void SetFadeInDuration(UIElement e, double value)
        {
            if (e == null) throw new ArgumentNullException("element");
            e.SetValue(FadeInDurationProperty, value);
        }

        public static readonly DependencyProperty FadeOutDurationProperty = DependencyProperty.RegisterAttached("FadeOutDuration", typeof(double), typeof(VisibilityAnimation), new PropertyMetadata(0.1));
        public static double GetFadeOutDuration(UIElement e)
        {
            if (e == null) throw new ArgumentNullException("element");
            return (double)e.GetValue(FadeOutDurationProperty);
        }
        public static void SetFadeOutDuration(UIElement e, double value)
        {
            if (e == null) throw new ArgumentNullException("element");
            e.SetValue(FadeOutDurationProperty, value);
        }

        #endregion

        #region Callbacks

        private static void VisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // So what? Ignore.
            // We only specified a property changed call-back to be able to set a coercion call-back
        }

        private static void OnIsActivePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Get the framework element and leave if it is null
            var fe = d as FrameworkElement;
            if (fe == null) return;

            // Hook the element if IsActive is true and unhook the element if it is false
            if (GetIsActive(fe)) HookedElements.Add(fe);
            else HookedElements.Remove(fe);
        }

        private static object CoerceVisibility(DependencyObject d, object baseValue)
        {
            if (SurpressEvent) return baseValue;  // Ignore coercion if we set the SurpressEvent flag

            var FE = d as FrameworkElement;
            if (FE == null || !HookedElements.Contains(FE)) return baseValue;  // Leave if the element is null or does not belong to our list of hooked elements

            Running = true;  // Set the running flag so that an animation does not change the visibility if another animation was started (Changing Visibility before the 1st animation completed)

            // If we get here, it means we have to start fade in or fade out animation
            // In any case return value of this method will be Visibility.Visible

            Visibility NewValue = (Visibility)baseValue;  // Get the new value

            if (NewValue == Visibility.Visible) FadeAnimation.Duration = new Duration(TimeSpan.FromSeconds((double)d.GetValue(FadeInDurationProperty)));  // Get the duration that was set for fade in
            else FadeAnimation.Duration = new Duration(TimeSpan.FromSeconds((double)d.GetValue(FadeOutDurationProperty)));  // Get the duration that was set for fade out

            // Use an anonymous method to set the Visibility to the new value after the animation completed
            FadeAnimation.Completed += (obj, args) =>
            {
                if (FE.Visibility != NewValue && !Running)
                {
                    SurpressEvent = true;  // SuppressEvent flag to skip coercion
                    FE.Visibility = NewValue;
                    SurpressEvent = false;
                    Running = false;  // Animation and Visibility change is now complete
                }
            };

            FadeAnimation.To = (NewValue == Visibility.Collapsed || NewValue == Visibility.Hidden) ? 0 : 1;  // Set the to value based on Visibility

            FE.BeginAnimation(UIElement.OpacityProperty, FadeAnimation);  // Start the animation (it will only start after we leave the coercion method)

            return Visibility.Visible;  // We need to return Visible in order to see the fading take place, otherwise it just sets it to Collapsed/Hidden without showing the animation
        }

        #endregion

        static VisibilityAnimation()
        {
            // Listen for visibility changes on all elements
            UIElement.VisibilityProperty.AddOwner(typeof(FrameworkElement), new FrameworkPropertyMetadata(Visibility.Visible, new PropertyChangedCallback(VisibilityChanged), CoerceVisibility));
        }
    }
}
