using System;
using AutoNotify;

namespace GeneratedDemo
{
    // The view model we'd like to augment
    public partial class ExampleViewModel
    {
        [AutoNotify]
        private string _text = "private field text";

        [AutoNotify(PropertyName = "Count")]
        private int _amount = 5;
    }

    public static class UseAutoNotifyGenerator
    {
        public static void Run()
        {
            ExampleViewModel vm = new ExampleViewModel();

            // we didn't explicitly create the 'Text' property, it was generated for us 
            string text = vm.Text;
            Console.WriteLine($"Text = {text}");

            // Properties can have differnt names generated based on the PropertyName argument of the attribute
            int count = vm.Count;
            Console.WriteLine($"Count = {count}");

            // the viewmodel will automatically implement INotifyPropertyChanged
            vm.PropertyChanged += (o, e) => Console.WriteLine($"Property {e.PropertyName} was changed");
            vm.Text = "abc";
            vm.Count = 123;

            // Try adding fields to the ExampleViewModel class above and tagging them with the [AutoNotify] attribute
            // You'll see the matching generated properties visibile in IntelliSense in realtime
        }
    }
}
