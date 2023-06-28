using System;
using UnityEngine.UIElements;

namespace Unithereum.Samples.Controls
{
    /// <summary>
    /// Base class for panels that allow for event handling.
    /// </summary>
    public abstract class Panel : VisualElement
    {
        /// <summary>
        /// Event to be called when this panel is showed.
        /// </summary>
        public event Action<Panel>? OnShow;

        /// <summary>
        /// The <see cref="VisualElement"/> this panel is to be contained in.
        /// </summary>
        protected VisualElement Container { get; set; } = null!;

        /// <summary>
        /// Hides all other panel in the container and shows this panel.
        /// </summary>
        public void Show()
        {
            if (this.IsShown)
                return;
            this.Container.Clear();
            this.Container.Add(this);
            this.OnShow?.Invoke(this);
        }

        /// <summary>
        /// Whether this panel is being shown.
        /// </summary>
        public bool IsShown => this.Container.Contains(this);

        /// <summary>
        /// An <see cref="IUxmlAttributes"/> implementation for initializing this panel from code.
        /// </summary>
        protected class DummyUxmlAttributes : IUxmlAttributes
        {
            /// <summary>
            /// An instance of <see cref="DummyUxmlAttributes"/>.
            /// </summary>
            public static readonly DummyUxmlAttributes Instance = new DummyUxmlAttributes();

            /// <summary>
            /// Retrieves the value of the attribute with the given name and sets the value to the out parameter.
            /// </summary>
            /// <param name="attributeName">The name of the attribute to retrieve.</param>
            /// <param name="value">The variable that will be set to the retrieved value.</param>
            /// <returns>Whether the value has been successfully retrieved.</returns>
            public bool TryGetAttributeValue(string attributeName, out string? value)
            {
                value = null;
                return false;
            }

            /// <summary>
            /// An private constructor to hide the default constructor.
            /// </summary>
            private DummyUxmlAttributes() { }
        }
    }
}
