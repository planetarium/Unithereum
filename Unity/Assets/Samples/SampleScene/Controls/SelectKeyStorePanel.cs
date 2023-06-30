#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unithereum.Samples.Controls
{
    /// <summary>
    /// A <see cref="Unithereum.Samples.Controls.Panel"/> that allows for selecting a keystore file and unlocking it.
    /// </summary>
    public class SelectKeyStorePanel : Panel
    {
        /// <summary>
        /// The <see cref="UxmlFactory{TCreatedType,TTraits}"/> that creates instances of
        /// <see cref="SelectKeyStorePanel"/> from UXML.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<SelectKeyStorePanel>
        {
            /// <summary>
            /// Creates an instance of <see cref="SelectKeyStorePanel"/> from UXML.
            /// </summary>
            /// <param name="bag">An class that contains the attributes provided from UXML.</param>
            /// <param name="cc">The given <see cref="CreationContext"/>.</param>
            /// <returns>The created <see cref="SelectKeyStorePanel"/>.</returns>
            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                VisualElement ve;
                Button unlockButton;
                var panel = (base.Create(bag, cc) as SelectKeyStorePanel)!;

                panel.Add(
                    ve = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            justifyContent = Justify.SpaceBetween,
                            overflow = Overflow.Hidden,
                        },
                    }
                );
                ve.Add(
                    panel.KeyStoreSelectDropdown = new DropdownField
                    {
                        label = "Select Keystore",
                        style = { flexGrow = 1, width = 0, }
                    }
                );
                ve.Add(panel.GenerateKeyStoreButton = new Button { text = "Generate" });
                ve.Add(panel.ImportKeyStoreButton = new Button { text = "Import" });
                panel.Add(
                    panel.PassphraseField = new TextField
                    {
                        label = "Passphrase",
                        isPasswordField = true
                    }
                );
                panel.Add(unlockButton = new Button { text = "Unlock" });
                panel.Add(
                    panel.InvalidPassphraseLabel = new Label
                    {
                        text = "Invalid Passphrase!",
                        style =
                        {
                            unityTextAlign = TextAnchor.MiddleCenter,
                            visibility = Visibility.Hidden
                        }
                    }
                );
                unlockButton.clicked += () => panel.OnUnlock?.Invoke(panel);
                panel.PassphraseField.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return)
                        panel.OnUnlock?.Invoke(panel);
                });
                return panel;
            }
        }

        /// <summary>
        /// The event that is invoked when unlock is attempted.
        /// </summary>
        public event Action<SelectKeyStorePanel>? OnUnlock;

        /// <summary>
        /// The <see cref="DropdownField"/> that allows for selecting a keystore file.
        /// </summary>
        public DropdownField KeyStoreSelectDropdown { get; private set; } = null!;

        /// <summary>
        /// The <see cref="Button"/> that allows for generating a new keystore file.
        /// </summary>
        public Button GenerateKeyStoreButton { get; private set; } = null!;

        /// <summary>
        /// The <see cref="Button"/> that allows for importing a private key.
        /// </summary>
        public Button ImportKeyStoreButton { get; private set; } = null!;

        /// <summary>
        /// The <see cref="TextField"/> that allows for entering a passphrase.
        /// </summary>
        public TextField PassphraseField { get; private set; } = null!;

        /// <summary>
        /// The <see cref="Label"/> that is shown when the passphrase is invalid.
        /// </summary>
        public Label InvalidPassphraseLabel { get; private set; } = null!;

        /// <summary>
        /// Do not use this default constructor.
        /// </summary>
        [Obsolete("Use Instantiate() static method instead.", true)]
        public SelectKeyStorePanel() { }

        /// <summary>
        /// Instantiates a <see cref="SelectKeyStorePanel"/> from code.
        /// </summary>
        /// <param name="container">The <see cref="VisualElement"/> that will be containing this
        /// <see cref="SelectKeyStorePanel"/>.</param>
        /// <returns>The created <see cref="SelectKeyStorePanel"/>.</returns>
        public static SelectKeyStorePanel Instantiate(VisualElement container)
        {
            var newPanel = (
                new UxmlFactory().Create(DummyUxmlAttributes.Instance, new CreationContext())
                as SelectKeyStorePanel
            )!;
            newPanel.Container = container;
            return newPanel;
        }
    }
}
