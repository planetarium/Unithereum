using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unithereum.Samples.Controls
{
    /// <summary>
    /// A <see cref="Unithereum.Samples.Controls.Panel"/> that waits for a transaction receipt and displays the result.
    /// </summary>
    public class WaitReceiptPanel : Panel
    {
        /// <summary>
        /// The <see cref="UxmlFactory{TCreatedType,TTraits}"/> that creates instances of
        /// <see cref="WaitReceiptPanel"/> from UXML.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<WaitReceiptPanel>
        {
            /// <summary>
            /// Creates an instance of <see cref="WaitReceiptPanel"/> from UXML.
            /// </summary>
            /// <param name="bag">An class that contains the attributes provided from UXML.</param>
            /// <param name="cc">The given <see cref="CreationContext"/>.</param>
            /// <returns>The created <see cref="WaitReceiptPanel"/>.</returns>
            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                VisualElement ve;
                var panel = (base.Create(bag, cc) as WaitReceiptPanel)!;

                panel.Add(
                    panel.LoadingLabel = new Label
                    {
                        text = "Loading...",
                        style = { unityTextAlign = TextAnchor.MiddleCenter },
                    }
                );

                panel.Add(
                    panel.TransactionInfoContainer = new VisualElement
                    {
                        style = { justifyContent = Justify.Center },
                    }
                );
                panel.TransactionInfoContainer.Add(
                    new Label
                    {
                        text = "Transaction",
                        style = { unityTextAlign = TextAnchor.MiddleCenter },
                    }
                );
                panel.TransactionInfoContainer.Add(
                    panel.TransactionIdLabel = new Label
                    {
                        style = { unityTextAlign = TextAnchor.MiddleCenter }
                    }
                );
                panel.TransactionInfoContainer.Add(
                    new Label
                    {
                        text = "included in",
                        style = { unityTextAlign = TextAnchor.MiddleCenter },
                    }
                );
                panel.TransactionInfoContainer.Add(
                    panel.BlockNumberLabel = new Label
                    {
                        style = { unityTextAlign = TextAnchor.MiddleCenter }
                    }
                );

                panel.Add(
                    panel.ErrorLabel = new Label
                    {
                        style = { unityTextAlign = TextAnchor.MiddleCenter }
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
                ve.Add(panel.ShowTransactionButton = new Button { text = "Show Transaction", });
                ve.Add(panel.CloseButton = new Button { text = "Close", });

                return panel;
            }
        }

        /// <summary>
        /// The <see cref="Label"/> that displays the loading message.
        /// </summary>
        public Label LoadingLabel { get; private set; } = null!;

        /// <summary>
        /// The <see cref="VisualElement"/> that contains the transaction info.
        /// </summary>
        public VisualElement TransactionInfoContainer { get; private set; } = null!;

        /// <summary>
        /// The <see cref="Label"/> that displays the transaction id.
        /// </summary>
        public Label TransactionIdLabel { get; private set; } = null!;

        /// <summary>
        /// The <see cref="Label"/> that displays the block number that contains the transaction.
        /// </summary>
        public Label BlockNumberLabel { get; private set; } = null!;

        /// <summary>
        /// The <see cref="Label"/> that displays the error message.
        /// </summary>
        public Label ErrorLabel { get; private set; } = null!;

        /// <summary>
        /// The <see cref="Button"/> that shows the transaction in the blockchain explorer.
        /// </summary>
        public Button ShowTransactionButton { get; private set; } = null!;

        /// <summary>
        /// The <see cref="Button"/> that closes the panel.
        /// </summary>
        public Button CloseButton { get; private set; } = null!;

        /// <summary>
        /// Do not use this default constructor.
        /// </summary>
        [Obsolete("Use Instantiate() static method instead.", true)]
        public WaitReceiptPanel() { }

        /// <summary>
        /// Instantiates a <see cref="WaitReceiptPanel"/> from code.
        /// </summary>
        /// <param name="container">The <see cref="VisualElement"/> that will be containing this
        /// <see cref="WaitReceiptPanel"/>.</param>
        /// <returns>The created <see cref="WaitReceiptPanel"/>.</returns>
        public static WaitReceiptPanel Instantiate(VisualElement container)
        {
            var newPanel = (
                new UxmlFactory().Create(DummyUxmlAttributes.Instance, new CreationContext())
                as WaitReceiptPanel
            )!;
            newPanel.Container = container;
            return newPanel;
        }
    }
}
