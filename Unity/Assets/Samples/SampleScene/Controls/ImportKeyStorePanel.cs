#nullable enable
using System;
using System.Linq;
using Nethereum.KeyStore;
using Nethereum.Signer;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unithereum.Samples.Controls
{
    /// <summary>
    /// A <see cref="Unithereum.Samples.Controls.Panel"/> that imports a keystore from a new key.
    /// </summary>
    public class ImportKeyStorePanel : Panel
    {
        /// <summary>
        /// The <see cref="UxmlFactory{TCreatedType,TTraits}"/> that creates instances of
        /// <see cref="ImportKeyStorePanel"/> from UXML.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<ImportKeyStorePanel>
        {
            /// <summary>
            /// Creates an instance of <see cref="ImportKeyStorePanel"/> from UXML.
            /// </summary>
            /// <param name="bag">An class that contains the attributes provided from UXML.</param>
            /// <param name="cc">The given <see cref="CreationContext"/>.</param>
            /// <returns>The created <see cref="ImportKeyStorePanel"/>.</returns>
            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                VisualElement ve;
                var panel = (base.Create(bag, cc) as ImportKeyStorePanel)!;

                panel.Add(panel.privateKeyTextField = new TextField { label = "Private Key" });

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
                ve.Add(new Label { text = "The address of the newly imported keystore:" });
                ve.Add(panel.copyAddressButton = new Button { text = "Copy" });

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
                    var thisPanel = (sender as ImportKeyStorePanel)!;
                    thisPanel.privateKeyTextField.value = string.Empty;
                    thisPanel.addressLabel.text = "TBD";
                    thisPanel.copyAddressButton.SetEnabled(false);
                    thisPanel.passwordTextField.value = string.Empty;
                    thisPanel.confirmPasswordTextField.value = string.Empty;
                    thisPanel.confirmPasswordNotMatchingErrorLabel.style.visibility =
                        Visibility.Hidden;
                    thisPanel.createButton.SetEnabled(false);
                };

                panel.copyAddressButton.clicked += () =>
                    GUIUtility.systemCopyBuffer = panel.addressLabel.text;

                static bool CheckPrivateKeyString(string privateKeyString)
                {
                    if (privateKeyString.StartsWith("0x"))
                        privateKeyString = privateKeyString[2..];
                    if (privateKeyString.Length != 64)
                        return false;

                    // ReSharper disable StringLiteralTypo
                    return privateKeyString
                        .Select(c => "0123456789abcdefABCDEF".Contains(c))
                        .All(b => b);
                    // ReSharper restore StringLiteralTypo
                }

                panel.privateKeyTextField.RegisterValueChangedCallback(evt =>
                {
                    panel.createButton.SetEnabled(false);
                    panel.copyAddressButton.SetEnabled(false);
                    if (!CheckPrivateKeyString(evt.newValue))
                    {
                        panel.addressLabel.text = "Invalid private key string";
                        return;
                    }
                    panel.copyAddressButton.SetEnabled(true);
                    panel.addressLabel.text = new EthECKey(evt.newValue).GetPublicAddress();
                    if (panel.confirmPasswordTextField.value == string.Empty)
                        return;
                    if (panel.confirmPasswordTextField.value != panel.passwordTextField.value)
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

                panel.passwordTextField.RegisterValueChangedCallback(evt =>
                {
                    panel.createButton.SetEnabled(false);
                    panel.copyAddressButton.SetEnabled(false);
                    if (!CheckPrivateKeyString(panel.privateKeyTextField.value))
                    {
                        panel.addressLabel.text = "Invalid private key string";
                        return;
                    }
                    panel.copyAddressButton.SetEnabled(true);
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
                    panel.copyAddressButton.SetEnabled(false);
                    if (!CheckPrivateKeyString(panel.privateKeyTextField.value))
                    {
                        panel.addressLabel.text = "Invalid private key string";
                        return;
                    }
                    panel.copyAddressButton.SetEnabled(true);
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
                    if (!CheckPrivateKeyString(panel.privateKeyTextField.value))
                    {
                        panel.addressLabel.text = "Invalid private key string";
                        panel.createButton.SetEnabled(false);
                        panel.copyAddressButton.SetEnabled(false);
                        return;
                    }
                    if (panel.passwordTextField.text != panel.confirmPasswordTextField.text)
                    {
                        panel.confirmPasswordNotMatchingErrorLabel.style.visibility =
                            Visibility.Visible;
                        panel.createButton.SetEnabled(false);
                        panel.copyAddressButton.SetEnabled(false);
                        return;
                    }

                    var key = new EthECKey(panel.privateKeyTextField.value);
                    var json = KeyStoreService.EncryptAndGenerateDefaultKeyStoreAsJson(
                        panel.passwordTextField.text,
                        key.GetPrivateKeyAsBytes(),
                        key.GetPublicAddress()
                    );

                    panel.privateKeyTextField.value = "";
                    panel.OnPostCreateKeyStore?.Invoke(json);
                };

                return panel;
            }
        }

        private static readonly KeyStoreService KeyStoreService = new KeyStoreService();

        private TextField privateKeyTextField = null!;

        private Label addressLabel = null!;

        private Button copyAddressButton = null!;

        private TextField passwordTextField = null!;

        private TextField confirmPasswordTextField = null!;

        private Button createButton = null!;

        private Label confirmPasswordNotMatchingErrorLabel = null!;

        /// <summary>
        /// The <see cref="Button"/> that cancels the import and returns to the select keystore panel.
        /// </summary>
        public Button CancelButton { get; private set; } = null!;

        /// <summary>
        /// Creates and returns the keystore json string for the newly imported key.
        /// </summary>
        public event Action<string>? OnPostCreateKeyStore;

        /// <summary>
        /// Do not use this default constructor.
        /// </summary>
        [Obsolete("Use Instantiate() static method instead.", true)]
        public ImportKeyStorePanel() { }

        /// <summary>
        /// Instantiates a <see cref="ImportKeyStorePanel"/> from code.
        /// </summary>
        /// <param name="container">The <see cref="VisualElement"/> that will be containing this
        /// <see cref="ImportKeyStorePanel"/>.</param>
        /// <returns>The created <see cref="ImportKeyStorePanel"/>.</returns>
        public static ImportKeyStorePanel Instantiate(VisualElement container)
        {
            var newPanel = (
                new UxmlFactory().Create(DummyUxmlAttributes.Instance, new CreationContext())
                as ImportKeyStorePanel
            )!;
            newPanel.Container = container;
            return newPanel;
        }
    }
}
