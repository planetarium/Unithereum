using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unithereum.Samples.Controls
{
    /// <summary>
    /// A <see cref="Unithereum.Samples.Controls.Panel"/> for initiating a transaction.
    /// </summary>
    public class InitiateTransactionPanel : Panel
    {
        /// <summary>
        /// The <see cref="UxmlFactory{TCreatedType,TTraits}"/> that creates instances of
        /// <see cref="InitiateTransactionPanel"/> from UXML.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<InitiateTransactionPanel>
        {
            /// <summary>
            /// Creates an instance of <see cref="InitiateTransactionPanel"/> from UXML.
            /// </summary>
            /// <param name="bag">An class that contains the attributes provided from UXML.</param>
            /// <param name="cc">The given <see cref="CreationContext"/>.</param>
            /// <returns>The created <see cref="InitiateTransactionPanel"/>.</returns>
            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                VisualElement ve;
                var panel = (base.Create(bag, cc) as InitiateTransactionPanel)!;

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
                    panel.RecipientField = new TextField
                    {
                        label = "Recipient",
                        style =
                        {
                            flexGrow = 1,
                            unityTextAlign = TextAnchor.UpperLeft,
                            width = 0,
                        }
                    }
                );
                ve.Add(panel.ClearRecipientButton = new Button { text = "×" });

                panel.Add(
                    ve = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            paddingLeft = new Length(3, LengthUnit.Pixel),
                            paddingRight = new Length(3, LengthUnit.Pixel),
                            paddingTop = new Length(3, LengthUnit.Pixel),
                            paddingBottom = new Length(3, LengthUnit.Pixel),
                            justifyContent = Justify.SpaceBetween,
                        },
                    }
                );
                ve.Add(
                    new Label
                    {
                        text = "Balance",
                        style = { unityTextAlign = TextAnchor.MiddleLeft },
                    }
                );
                ve.Add(
                    panel.BalanceLabel = new Label
                    {
                        style = { unityTextAlign = TextAnchor.MiddleLeft }
                    }
                );

                panel.Add(
                    ve = new VisualElement { style = { flexDirection = FlexDirection.Row, }, }
                );
                ve.Add(
                    panel.AmountField = new TextField { label = "Amount", style = { flexGrow = 1 } }
                );
                ve.Add(
                    panel.CurrencyLabel = new Label
                    {
                        text = "TBD",
                        style = { unityTextAlign = TextAnchor.MiddleLeft },
                    }
                );

                panel.Add(
                    ve = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            paddingLeft = new Length(3, LengthUnit.Pixel),
                            paddingRight = new Length(3, LengthUnit.Pixel),
                            paddingTop = new Length(3, LengthUnit.Pixel),
                            paddingBottom = new Length(3, LengthUnit.Pixel),
                            justifyContent = Justify.SpaceBetween
                        },
                    }
                );
                ve.Add(
                    new Label
                    {
                        text = "Estimated Gas",
                        style = { unityTextAlign = TextAnchor.MiddleLeft },
                    }
                );
                ve.Add(
                    panel.EstimatedGasLabel = new Label
                    {
                        text = "TBD",
                        style = { unityTextAlign = TextAnchor.MiddleLeft },
                    }
                );

                panel.Add(
                    ve = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            justifyContent = Justify.SpaceBetween,
                        },
                    }
                );
                ve.Add(
                    panel.CancelTransactionButton = new Button
                    {
                        text = "Cancel",
                        style = { flexGrow = 1 }
                    }
                );
                ve.Add(
                    panel.VerifyTransactionButton = new Button
                    {
                        text = "Send",
                        style = { flexGrow = 1 }
                    }
                );
                panel.Add(
                    panel.ErrorPanel = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            justifyContent = Justify.Center,
                            height = new Length(16, LengthUnit.Pixel),
                        },
                    }
                );
                panel.ErrorPanel.Add(
                    panel.RecipientErrorLabel = new Label
                    {
                        text = "Invalid recipient",
                        style = { display = DisplayStyle.None },
                    }
                );
                panel.ErrorPanel.Add(
                    panel.InvalidAmountErrorLabel = new Label
                    {
                        text = "Invalid amount",
                        style = { display = DisplayStyle.None },
                    }
                );
                panel.ErrorPanel.Add(
                    panel.InsufficientFundsErrorLabel = new Label
                    {
                        text = "Insufficient funds",
                        style = { display = DisplayStyle.None },
                    }
                );
                panel.ErrorPanel.Add(
                    panel.InsufficientGasErrorLabel = new Label
                    {
                        text = "Insufficient funds for gas",
                        style = { display = DisplayStyle.None },
                    }
                );
                panel.ErrorPanel.Add(
                    panel.NotApprovedErrorLabel = new Label
                    {
                        text = "Signer not approved for action",
                        style = { display = DisplayStyle.None },
                    }
                );
                panel.ErrorPanel.Add(
                    panel.NotOwnerErrorLabel = new Label
                    {
                        text = "Only owner can perform action",
                        style = { display = DisplayStyle.None },
                    }
                );
                panel.ErrorPanel.Add(
                    panel.NetworkErrorLabel = new Label
                    {
                        text = "Network error",
                        style = { display = DisplayStyle.None },
                    }
                );
                panel.ErrorPanel.Add(
                    panel.NodeErrorLabel = new Label
                    {
                        text = "Node returned error",
                        style = { display = DisplayStyle.None },
                    }
                );

                return panel;
            }
        }

        /// <summary>
        /// The text field for the recipient address.
        /// </summary>
        public TextField RecipientField { get; private set; } = null!;

        /// <summary>
        /// The button to clear the recipient address.
        /// </summary>
        public Button ClearRecipientButton { get; private set; } = null!;

        /// <summary>
        /// The label that displays the balance of the selected account.
        /// </summary>
        public Label BalanceLabel { get; private set; } = null!;

        /// <summary>
        /// The text field for the amount to send.
        /// </summary>
        public TextField AmountField { get; private set; } = null!;

        /// <summary>
        /// The label that displays the ticker of the selected token.
        /// </summary>
        public Label CurrencyLabel { get; private set; } = null!;

        /// <summary>
        /// The label that displays the estimated gas cost of the transaction.
        /// </summary>
        public Label EstimatedGasLabel { get; private set; } = null!;

        /// <summary>
        /// The label that displays the error message for an invalid recipient address.
        /// </summary>
        public Label RecipientErrorLabel { get; private set; } = null!;

        /// <summary>
        /// The label that displays the error message for an invalid amount.
        /// </summary>
        public Label InvalidAmountErrorLabel { get; private set; } = null!;

        /// <summary>
        /// The label that displays the error message for insufficient funds.
        /// </summary>
        public Label InsufficientFundsErrorLabel { get; private set; } = null!;

        /// <summary>
        /// The label that displays the error message for insufficient funds for gas.
        /// </summary>
        public Label InsufficientGasErrorLabel { get; private set; } = null!;

        /// <summary>
        /// The label that displays the error message for the signer not being approved for the action.
        /// </summary>
        public Label NotApprovedErrorLabel { get; private set; } = null!;

        /// <summary>
        /// The label that displays the error message for the signer not being the owner.
        /// </summary>
        public Label NotOwnerErrorLabel { get; private set; } = null!;

        /// <summary>
        /// The label that displays the error message for a network error.
        /// </summary>
        public Label NetworkErrorLabel { get; private set; } = null!;

        /// <summary>
        /// The label that displays the error message for a node error.
        /// </summary>
        public Label NodeErrorLabel { get; private set; } = null!;

        /// <summary>
        /// The button to verify the transaction.
        /// </summary>
        public Button VerifyTransactionButton { get; private set; } = null!;

        /// <summary>
        /// The button that cancels initiating the transaction and returns to the <see cref="BalancePanel" />.
        /// </summary>
        public Button CancelTransactionButton { get; private set; } = null!;

        /// <summary>
        /// Container for error messages.
        /// </summary>
        private VisualElement ErrorPanel { get; set; } = null!;

        /// <summary>
        /// Do not use this default constructor.
        /// </summary>
        [Obsolete("Use Instantiate() static method instead.", true)]
        public InitiateTransactionPanel() { }

        /// <summary>
        /// Instantiates a <see cref="InitiateTransactionPanel"/> from code.
        /// </summary>
        /// <param name="container">The <see cref="VisualElement"/> that will be containing this
        /// <see cref="InitiateTransactionPanel"/>.</param>
        /// <returns>The created <see cref="InitiateTransactionPanel"/>.</returns>
        public static InitiateTransactionPanel Instantiate(VisualElement container)
        {
            var newPanel = (
                new UxmlFactory().Create(DummyUxmlAttributes.Instance, new CreationContext())
                as InitiateTransactionPanel
            )!;
            newPanel.Container = container;
            return newPanel;
        }

        /// <summary>
        /// Clears all error messages.
        /// </summary>
        public void ClearError() =>
            this.ErrorPanel
                .Query<Label>()
                .ForEach(label => label.style.display = DisplayStyle.None);
    }
}
