using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unithereum.Samples.Controls
{
    /// <summary>
    /// A <see cref="Unithereum.Samples.Controls.Panel"/> that displays the balance of an account.
    /// </summary>
    public class BalancePanel : Panel
    {
        /// <summary>
        /// The <see cref="UxmlFactory{TCreatedType,TTraits}"/> that creates instances of
        /// <see cref="BalancePanel"/> from UXML.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<BalancePanel>
        {
            /// <summary>
            /// Creates an instance of <see cref="BalancePanel"/> from UXML.
            /// </summary>
            /// <param name="bag">An class that contains the attributes provided from UXML.</param>
            /// <param name="cc">The given <see cref="CreationContext"/>.</param>
            /// <returns>The created <see cref="BalancePanel"/>.</returns>
            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                VisualElement ve,
                    veChild;
                var panel = (base.Create(bag, cc) as BalancePanel)!;

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
                ve.Add(
                    veChild = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            justifyContent = Justify.FlexStart
                        },
                    }
                );
                veChild.Add(
                    new Label
                    {
                        text = "Account ",
                        style = { unityTextAlign = TextAnchor.MiddleLeft }
                    }
                );
                veChild.Add(
                    panel.AddressLabel = new Label
                    {
                        style = { unityTextAlign = TextAnchor.MiddleLeft }
                    }
                );

                ve.Add(
                    veChild = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            justifyContent = Justify.FlexStart
                        },
                    }
                );
                veChild.Add(panel.CopyAddressButton = new Button { text = "Copy" });
                veChild.Add(panel.LockButton = new Button { text = "Lock" });

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
                ve.Add(
                    veChild = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            justifyContent = Justify.FlexStart
                        },
                    }
                );
                veChild.Add(
                    new Label
                    {
                        text = "Balance: ",
                        style = { unityTextAlign = TextAnchor.MiddleLeft }
                    }
                );
                veChild.Add(
                    panel.BalanceLabel = new Label
                    {
                        text = "Loading...",
                        style = { unityTextAlign = TextAnchor.MiddleLeft }
                    }
                );

                ve.Add(panel.InitiateSendButton = new Button { text = "Send" });

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
                ve.Add(
                    veChild = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            justifyContent = Justify.FlexStart
                        },
                    }
                );
                veChild.Add(
                    panel.ERC20NameLabel = new Label
                    {
                        text = "Loading...",
                        style = { unityTextAlign = TextAnchor.MiddleLeft, paddingRight = 0 }
                    }
                );
                veChild.Add(
                    new Label
                    {
                        text = ":",
                        style = { unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = 0 }
                    }
                );
                veChild.Add(
                    panel.ERC20BalanceLabel = new Label
                    {
                        text = "Loading...",
                        style = { unityTextAlign = TextAnchor.MiddleLeft }
                    }
                );

                ve.Add(
                    veChild = new VisualElement { style = { flexDirection = FlexDirection.Row }, }
                );
                veChild.Add(panel.MintERC20Button = new Button { text = "Mint" });
                veChild.Add(panel.SendERC20Button = new Button { text = "Send" });
                veChild.Add(panel.BurnERC20Button = new Button { text = "Burn" });

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
                ve.Add(
                    veChild = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            justifyContent = Justify.FlexStart
                        },
                    }
                );
                veChild.Add(
                    new Label
                    {
                        text = "ERC-1155 (id 21155):",
                        style = { unityTextAlign = TextAnchor.MiddleLeft }
                    }
                );
                veChild.Add(
                    panel.ERC1155BalanceLabel = new Label
                    {
                        text = "Loading...",
                        style = { unityTextAlign = TextAnchor.MiddleLeft }
                    }
                );

                ve.Add(
                    veChild = new VisualElement { style = { flexDirection = FlexDirection.Row }, }
                );
                veChild.Add(panel.MintERC1155Button = new Button { text = "Mint" });
                veChild.Add(panel.SendERC1155Button = new Button { text = "Send" });
                veChild.Add(panel.BurnERC1155Button = new Button { text = "Burn" });

                return panel;
            }
        }

        /// <summary>
        /// The label that displays the account address.
        /// </summary>
        public Label AddressLabel { get; private set; } = null!;

        /// <summary>
        /// The label that displays the account balance of the native token.
        /// </summary>
        public Label BalanceLabel { get; private set; } = null!;

        // ReSharper disable InconsistentNaming
        /// <summary>
        /// the label that displays the name of the ERC-20 token retrieved from the contract.
        /// </summary>
        public Label ERC20NameLabel { get; private set; } = null!;

        /// <summary>
        /// the label that displays the balance of the ERC-20 token.
        /// </summary>
        public Label ERC20BalanceLabel { get; private set; } = null!;

        /// <summary>
        /// The label that displays the balance of the ERC-1155 token.
        /// </summary>
        public Label ERC1155BalanceLabel { get; private set; } = null!;

        // ReSharper restore InconsistentNaming

        /// <summary>
        /// The button that copies the account address to the clipboard.
        /// </summary>
        public Button CopyAddressButton { get; private set; } = null!;

        /// <summary>
        /// The button that locks the account and returns to the <see cref="SelectKeyStorePanel"/>.
        /// </summary>
        public Button LockButton { get; private set; } = null!;

        /// <summary>
        /// The button that initiates native token transfer.
        /// </summary>
        public Button InitiateSendButton { get; private set; } = null!;

        // ReSharper disable InconsistentNaming
        /// <summary>
        /// The button that initiates ERC-20 token minting.
        /// </summary>
        public Button MintERC20Button { get; private set; } = null!;

        /// <summary>
        /// The button that initiates ERC-20 token transfer.
        /// </summary>
        public Button SendERC20Button { get; private set; } = null!;

        /// <summary>
        /// The button that initiates ERC-20 token burning.
        /// </summary>
        public Button BurnERC20Button { get; private set; } = null!;

        /// <summary>
        /// The button that initiates ERC-1155 token minting.
        /// </summary>
        public Button MintERC1155Button { get; private set; } = null!;

        /// <summary>
        /// The button that initiates ERC-1155 token transfer.
        /// </summary>
        public Button SendERC1155Button { get; private set; } = null!;

        /// <summary>
        /// The button that initiates ERC-1155 token burning.
        /// </summary>
        public Button BurnERC1155Button { get; private set; } = null!;

        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Do not use this default constructor.
        /// </summary>
        [Obsolete("Use Instantiate() static method instead.", true)]
        public BalancePanel() { }

        /// <summary>
        /// Instantiates a <see cref="BalancePanel"/> from code.
        /// </summary>
        /// <param name="container">The <see cref="VisualElement"/> that will be containing this
        /// <see cref="BalancePanel"/>.</param>
        /// <returns>The created <see cref="BalancePanel"/>.</returns>
        public static BalancePanel Instantiate(VisualElement container)
        {
            var newPanel = (
                new UxmlFactory().Create(DummyUxmlAttributes.Instance, new CreationContext())
                as BalancePanel
            )!;
            newPanel.Container = container;
            return newPanel;
        }
    }
}
