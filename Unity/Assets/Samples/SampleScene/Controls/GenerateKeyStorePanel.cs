#nullable enable
using System;
using Nethereum.KeyStore;
using Nethereum.Signer;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unithereum.Samples.Controls
{
    /// <summary>
    /// A <see cref="Unithereum.Samples.Controls.Panel"/> that generates a keystore from a new key.
    /// </summary>
    public class GenerateKeyStorePanel : Panel
    {
        /// <summary>
        /// The <see cref="UxmlFactory{TCreatedType,TTraits}"/> that creates instances of
        /// <see cref="GenerateKeyStorePanel"/> from UXML.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<GenerateKeyStorePanel>
        {
            /// <summary>
            /// Creates an instance of <see cref="GenerateKeyStorePanel"/> from UXML.
            /// </summary>
            /// <param name="bag">An class that contains the attributes provided from UXML.</param>
            /// <param name="cc">The given <see cref="CreationContext"/>.</param>
            /// <returns>The created <see cref="GenerateKeyStorePanel"/>.</returns>
            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                VisualElement ve;
                Button copyAddressButton;
                var panel = (base.Create(bag, cc) as GenerateKeyStorePanel)!;

                panel.Add(
                    ve = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            justifyContent = Justify.SpaceBetween
                        },
                    }
                );
                ve.Add(new Label { text = "The address of the newly generated keystore:" });
                ve.Add(copyAddressButton = new Button { text = "Copy" });

                panel.Add(panel.addressLabel = new Label());

                panel.Add(
                    panel.passwordTextField = new TextField
                    {
                        label = "Password",
                        isPasswordField = true
                    }
                );

                panel.Add(
                    panel.confirmPasswordTextField = new TextField
                    {
                        label = "Confirm Password",
                        isPasswordField = true
                    }
                );

                panel.Add(
                    ve = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            justifyContent = Justify.Center
                        },
                    }
                );
                ve.Add(panel.CancelButton = new Button { text = "Cancel" });
                ve.Add(panel.createButton = new Button { text = "Create" });

                panel.Add(
                    panel.confirmPasswordNotMatchingErrorLabel = new Label
                    {
                        text = "The passwords do not match.",
                        style =
                        {
                            unityTextAlign = TextAnchor.MiddleCenter,
                            visibility = Visibility.Hidden,
                        },
                    }
                );

                panel.OnShow += sender =>
                {
                    var thisPanel = (sender as GenerateKeyStorePanel)!;
                    thisPanel.key = EthECKey.GenerateKey();
                    thisPanel.addressLabel.text = thisPanel.key.GetPublicAddress();
                    thisPanel.passwordTextField.value = string.Empty;
                    thisPanel.confirmPasswordTextField.value = string.Empty;
                    thisPanel.confirmPasswordNotMatchingErrorLabel.style.visibility =
                        Visibility.Hidden;
                    thisPanel.createButton.SetEnabled(false);
                };

                copyAddressButton.clicked += () =>
                    GUIUtility.systemCopyBuffer = panel.addressLabel.text;

                panel.passwordTextField.RegisterValueChangedCallback(evt =>
                {
                    panel.createButton.SetEnabled(false);
                    if (panel.confirmPasswordTextField.value == string.Empty)
                        return;
                    if (panel.confirmPasswordTextField.value != evt.newValue)
                    {
                        panel.confirmPasswordNotMatchingErrorLabel.style.visibility =
                            Visibility.Visible;
                    }
                    else
                    {
                        panel.confirmPasswordNotMatchingErrorLabel.style.visibility =
                            Visibility.Hidden;
                        panel.createButton.SetEnabled(true);
                    }
                });

                panel.confirmPasswordTextField.RegisterValueChangedCallback(evt =>
                {
                    panel.createButton.SetEnabled(false);
                    if (panel.passwordTextField.value != evt.newValue)
                    {
                        panel.confirmPasswordNotMatchingErrorLabel.style.visibility =
                            Visibility.Visible;
                    }
                    else
                    {
                        panel.confirmPasswordNotMatchingErrorLabel.style.visibility =
                            Visibility.Hidden;
                        panel.createButton.SetEnabled(true);
                    }
                });

                panel.createButton.clicked += () =>
                {
                    if (panel.passwordTextField.text != panel.confirmPasswordTextField.text)
                    {
                        panel.confirmPasswordNotMatchingErrorLabel.style.visibility =
                            Visibility.Visible;
                        return;
                    }

                    var json = KeyStoreService.EncryptAndGenerateDefaultKeyStoreAsJson(
                        panel.passwordTextField.text,
                        panel.key!.GetPrivateKeyAsBytes(),
                        panel.key!.GetPublicAddress()
                    );

                    panel.key = null;
                    panel.OnPostCreateKeyStore?.Invoke(json);
                };

                return panel;
            }
        }

        private static readonly KeyStoreService KeyStoreService = new KeyStoreService();

        private EthECKey? key;

        private Label addressLabel = null!;

        private TextField passwordTextField = null!;

        private TextField confirmPasswordTextField = null!;

        private Button createButton = null!;

        private Label confirmPasswordNotMatchingErrorLabel = null!;

        /// <summary>
        /// The <see cref="Button"/> that cancels the generation and returns to the select keystore panel.
        /// </summary>
        public Button CancelButton { get; private set; } = null!;

        /// <summary>
        /// Creates and returns the keystore json string for the newly generated key.
        /// </summary>
        public event Action<string>? OnPostCreateKeyStore;

        /// <summary>
        /// Do not use this default constructor.
        /// </summary>
        [Obsolete("Use Instantiate() static method instead.", true)]
        public GenerateKeyStorePanel() { }

        /// <summary>
        /// Instantiates a <see cref="GenerateKeyStorePanel"/> from code.
        /// </summary>
        /// <param name="container">The <see cref="VisualElement"/> that will be containing this
        /// <see cref="GenerateKeyStorePanel"/>.</param>
        /// <returns>The created <see cref="GenerateKeyStorePanel"/>.</returns>
        public static GenerateKeyStorePanel Instantiate(VisualElement container)
        {
            var newPanel = (
                new UxmlFactory().Create(DummyUxmlAttributes.Instance, new CreationContext())
                as GenerateKeyStorePanel
            )!;
            newPanel.Container = container;
            return newPanel;
        }
    }
}
