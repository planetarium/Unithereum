using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.JsonRpc.Client;
using Nethereum.KeyStore;
using Nethereum.KeyStore.Crypto;
using Nethereum.RPC.TransactionManagers;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using Unithereum.ContractServices.ERC20Token;
using Unithereum.ContractServices.ERC1155Token;
using Unithereum.Samples.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using ERC20TokenDefinition = Unithereum.ContractServices.ERC20Token.ContractDefinition;
using ERC1155TokenDefinition = Unithereum.ContractServices.ERC1155Token.ContractDefinition;

namespace Unithereum.Samples
{
    /// <summary>
    /// A sample UI that uses Unithereum to implement a simple wallet.
    /// </summary>
    public class SampleUI : MonoBehaviour
    {
        /// <summary>
        /// Format of a
        /// <a href="https://ethereum.org/en/developers/docs/data-structures-and-encoding/web3-secret-storage/">
        /// Web3 Secret Storage</a> filename.
        /// </summary>
        private static readonly string NameFormat = "UTC--{0:yyyy-MM-dd}T{0:HH-mm-ss}Z--{1:D}";

        /// <summary>
        /// Pattern of a
        /// <a href="https://ethereum.org/en/developers/docs/data-structures-and-encoding/web3-secret-storage/">
        /// Web3 Secret Storage</a> filename.
        /// </summary>
        private static readonly Regex KeyStoreFilenamePattern =
            new(
                @"^UTC--\d{4}-\d\d-\d\dT\d\d-\d\d-\d\dZ--([\da-f]{8}-?(?:[\da-f]{4}-?){3}[\da-f]{12})$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
            );

        private static readonly KeyStoreService KeyStoreService = new();

        /// <summary>
        /// The default keystore path, resolved to a path in the persistent data path.
        /// </summary>
        private static string KeyStorePath =>
            Path.Combine(Application.persistentDataPath, "keystore");

        private Account account = null!;

        private Web3 web3 = null!;

        private IEtherTransferService ethTransferService = null!;

        /// <summary>
        /// Service for interacting with the ERC20 contract.
        /// </summary>
        private ERC20TokenService erc20Service = null!;

        /// <summary>
        /// Service for interacting with the ERC1155 contract.
        /// </summary>
        private ERC1155TokenService erc1155Service = null!;

        // ReSharper disable once IdentifierTypo
        /// <summary>
        /// Dictionary of keystore IDs to keystore JSONs.
        /// </summary>
        private Dictionary<string, string> keystores = null!;

        private SelectKeyStorePanel selectKeyStorePanel = null!;

        private GenerateKeyStorePanel generateKeyStorePanel = null!;

        private ImportKeyStorePanel importKeyStorePanel = null!;

        private BalancePanel balancePanel = null!;

        private InitiateTransactionPanel initiateTransactionPanel = null!;

        private WaitReceiptPanel waitReceiptPanel = null!;

        /// <summary>
        /// Enum of current transaction mode used in the <see cref="initiateTransactionPanel"/>.
        /// </summary>
        private TransactionMode txMode = TransactionMode.TransferEth;

        /// <summary>
        /// Event used to update the estimated gas label instead of an await call that cannot update the UI in Unity.
        /// </summary>
        private event Action UpdateEstimatedGas = null!;

        /// <summary>
        /// Event used to update the balance label instead of an await call that cannot update the UI in Unity.
        /// </summary>
        private event Action UpdateFromBalance = null!;

        /// <summary>
        /// Enumerates the pair of key IDs and paths in the keystore directory.
        /// </summary>
        /// <returns>An IEnumerable containing the pairs of key ID and path.</returns>
        private static IEnumerable<(Guid, string)> ListKeyStoreFiles()
        {
            IEnumerable<string> keyPaths = Directory.EnumerateFiles(KeyStorePath);
            foreach (var keyPath in keyPaths)
            {
                if (Path.GetFileName(keyPath) is not { } f)
                    continue;

                var m = KeyStoreFilenamePattern.Match(f);

                if (!m.Success)
                    continue;

                if (!Guid.TryParse(m.Groups[1].Value, out var id))
                    continue;

                yield return (id, keyPath);
            }
        }

        /// <summary>
        /// Calls the contract and returns the result or an error message string.
        /// </summary>
        /// <param name="function">An async function that calls the contract and returns a result string.</param>
        /// <returns>Result of the <see cref="function"/> or an error message string.</returns>
        private static async Task<string> TryCallContract(Func<Task<string>> function)
        {
            try
            {
                return await function();
            }
            catch (SocketException)
            {
                return "Network error";
            }
            catch (HttpRequestException)
            {
                return "Node error";
            }
        }

        // Start is called before the first frame update
        private void Start()
        {
            var root = this.GetComponent<UIDocument>().rootVisualElement;
            root.style.backgroundColor = new Color(241, 240, 240);
            root.style.justifyContent = Justify.Center;

            this.selectKeyStorePanel = SelectKeyStorePanel.Instantiate(root);
            this.selectKeyStorePanel.KeyStoreSelectDropdown.RegisterValueChangedCallback(evt =>
            {
                PlayerPrefs.SetString("selectedKeyStore", evt.newValue);
            });
            this.selectKeyStorePanel.OnShow += this.RefreshKeyStorePanel;
            this.selectKeyStorePanel.OnUnlock += this.Unlock;
            this.selectKeyStorePanel.GenerateKeyStoreButton.clicked += () =>
                this.generateKeyStorePanel.Show();
            this.selectKeyStorePanel.ImportKeyStoreButton.clicked += () =>
                this.importKeyStorePanel.Show();

            this.generateKeyStorePanel = GenerateKeyStorePanel.Instantiate(root);
            this.generateKeyStorePanel.OnPostCreateKeyStore += this.PostCreateKeyStore;
            this.generateKeyStorePanel.CancelButton.clicked += () =>
                this.selectKeyStorePanel.Show();

            this.importKeyStorePanel = ImportKeyStorePanel.Instantiate(root);
            this.importKeyStorePanel.OnPostCreateKeyStore += this.PostCreateKeyStore;
            this.importKeyStorePanel.CancelButton.clicked += () => this.selectKeyStorePanel.Show();

            this.balancePanel = BalancePanel.Instantiate(root);
            this.balancePanel.OnShow += this.RefreshBalancePanel;
            this.balancePanel.CopyAddressButton.clicked += () =>
                GUIUtility.systemCopyBuffer = this.balancePanel.AddressLabel.text;
            this.balancePanel.LockButton.clicked += this.Lock;
            this.balancePanel.InitiateSendButton.clicked += () => this.InitiateTransfer(TransactionMode.TransferEth);
            this.balancePanel.MintERC20Button.clicked += () => this.InitiateTransfer(TransactionMode.MintERC20);
            this.balancePanel.SendERC20Button.clicked += () => this.InitiateTransfer(TransactionMode.TransferERC20);
            this.balancePanel.BurnERC20Button.clicked += () => this.InitiateTransfer(TransactionMode.BurnERC20);
            this.balancePanel.MintERC1155Button.clicked += () => this.InitiateTransfer(TransactionMode.MintERC1155);
            this.balancePanel.SendERC1155Button.clicked += () => this.InitiateTransfer(TransactionMode.TransferERC1155);
            this.balancePanel.BurnERC1155Button.clicked += () => this.InitiateTransfer(TransactionMode.BurnERC1155);

            this.initiateTransactionPanel = InitiateTransactionPanel.Instantiate(root);
            this.initiateTransactionPanel.OnShow += this.InitializeInitiateTransactionPanel;
            this.initiateTransactionPanel.ClearRecipientButton.clicked += () => this.ClearRecipient(this.initiateTransactionPanel);
            this.initiateTransactionPanel.CancelTransactionButton.clicked += () =>
                this.balancePanel.Show();

            this.UpdateFromBalance += async () =>
                await this.HandleUpdateFromBalance(this.initiateTransactionPanel);
            this.UpdateEstimatedGas += async () =>
                await this.HandleUpdateEstimatedGas(this.initiateTransactionPanel);
            this.initiateTransactionPanel.RecipientField.RegisterValueChangedCallback(this.HandleRecipientFieldChange(this.initiateTransactionPanel)
            );
            this.initiateTransactionPanel.AmountField.RegisterValueChangedCallback(this.HandleAmountFieldChange()
            );

            this.initiateTransactionPanel.VerifyTransactionButton.clicked += this.VerifyTransaction;

            this.waitReceiptPanel = WaitReceiptPanel.Instantiate(root);
            this.waitReceiptPanel.OnShow += this.ResetWaitReceiptPanel;
            this.waitReceiptPanel.ShowTransactionButton.clicked += () =>
                Application.OpenURL(
                    string.Format(
                        Config.BlockExplorerTxTemplate,
                        this.waitReceiptPanel.TransactionIdLabel.text
                    )
                );
            this.waitReceiptPanel.CloseButton.clicked += () => this.balancePanel.Show();

            // Show SelectKeyStorePanel
            this.selectKeyStorePanel.Show();
        }

        /// <summary>
        /// Refreshes the keystore panel.
        /// </summary>
        /// <param name="sender">The keystore panel being refreshed.</param>
        private void RefreshKeyStorePanel(Panel sender)
        {
            var panel = (sender as SelectKeyStorePanel)!;
            if (!Directory.Exists(KeyStorePath))
            {
                Directory.CreateDirectory(KeyStorePath);
            }

            this.keystores = ListKeyStoreFiles()
                .Select<(Guid id, string path), (string address, string json)?>(file =>
                {
                    var json = File.ReadAllText(file.path);
                    string address;
                    try
                    {
                        address = KeyStoreService.GetAddressFromKeyStore(json);
                        address = address.StartsWith("0x") ? address : "0x" + address;
                    }
                    catch (JsonReaderException)
                    {
                        return null;
                    }

                    return (address, json);
                })
                .OfType<(string address, string json)>()
                .ToDictionary(tuple => tuple.address, tuple => tuple.json);

            panel.KeyStoreSelectDropdown.choices = this.keystores.Keys.ToList();
            panel.KeyStoreSelectDropdown.value =
                PlayerPrefs.GetString("selectedKeyStore", null)
                ?? panel.KeyStoreSelectDropdown.choices[0];
            panel.PassphraseField.value = "";
            panel.InvalidPassphraseLabel.style.visibility = Visibility.Hidden;
        }

        private void PostCreateKeyStore(string json)
        {
            var address = KeyStoreService.GetAddressFromKeyStore(json);
            File.WriteAllText(
                Path.Combine(
                    KeyStorePath,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        NameFormat,
                        DateTimeOffset.UtcNow,
                        Guid.NewGuid()
                    )
                ),
                json
            );
            PlayerPrefs.SetString("selectedKeyStore", address);
            this.selectKeyStorePanel.Show();
        }

        /// <summary>
        /// Unlocks and retrieves the account from the selected keystore.
        /// </summary>
        /// <param name="sender">The keystore panel from which the keystore is being unlocked.</param>
        private void Unlock(SelectKeyStorePanel sender)
        {
            // TODO: implement better error handling
            if (Config.ERC20Address == string.Empty || Config.ERC1155Address == string.Empty)
            {
                sender.InvalidPassphraseLabel.text =
                    "Please configure ERC20 and ERC1155 addresses in Config.cs";
                sender.InvalidPassphraseLabel.style.visibility = Visibility.Visible;
                return;
            }

            try
            {
                this.account = Account.LoadFromKeyStore(
                    this.keystores[sender.KeyStoreSelectDropdown.value],
                    sender.PassphraseField.value
                );
                this.web3 = new Web3(this.account, new RpcClient(new Uri(Config.JsonRpcEndpoint)));
                this.ethTransferService = this.web3.Eth.GetEtherTransferService();
                this.erc20Service = new ERC20TokenService(this.web3, Config.ERC20Address);
                this.erc1155Service = new ERC1155TokenService(this.web3, Config.ERC1155Address);
                this.balancePanel.Show();
            }
            catch (DecryptionException)
            {
                sender.InvalidPassphraseLabel.style.visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Refreshes the balance panel.
        /// </summary>
        /// <param name="sender">The balance panel being refreshed.</param>
        private async void RefreshBalancePanel(Panel sender)
        {
            var panel = (sender as BalancePanel)!;
            try
            {
                panel.AddressLabel.text = this.account.Address;
                panel.ERC20NameLabel.text = "Loading...";
                panel.BalanceLabel.text = "Loading...";
                panel.ERC20BalanceLabel.text = "Loading...";
                panel.ERC1155BalanceLabel.text = "Loading...";

                panel.ERC20NameLabel.text = await TryCallContract(
                    async () => await this.erc20Service.NameQueryAsync()
                );
                panel.BalanceLabel.text = await TryCallContract(
                    async () =>
                        Web3.Convert.FromWei(
                            (
                                await this.web3.Eth.GetBalance.SendRequestAsync(
                                    this.account.Address
                                )
                            ).Value
                        )
                        + " "
                        + Config.NativeTokenTicker
                );
                panel.ERC20BalanceLabel.text = await TryCallContract(async () =>
                {
                    var erc20Balance = Web3.Convert.FromWei(
                        await this.erc20Service.BalanceOfQueryAsync(this.account.Address),
                        await this.erc20Service.DecimalsQueryAsync()
                    );
                    var erc20Symbol = await this.erc20Service.SymbolQueryAsync();
                    return $"{erc20Balance} {erc20Symbol}";
                });
                panel.ERC1155BalanceLabel.text = await TryCallContract(async () =>
                {
                    var erc1155Balance = await this.erc1155Service.BalanceOfQueryAsync(
                        this.account.Address,
                        21155
                    );
                    return $"{Web3.Convert.FromWei(erc1155Balance)} * 10^18\n({erc1155Balance})";
                });
            }
            catch (NullReferenceException)
            {
                if (sender.IsShown)
                    throw;
            }
        }

        /// <summary>
        /// Locks the account and returns to the keystore panel.
        /// </summary>
        private void Lock()
        {
            this.web3 = null!;
            this.account = null!;
            this.erc20Service = null!;
            this.erc1155Service = null!;
            this.selectKeyStorePanel.Show();
        }

        /// <summary>
        /// Initializes the initiate transaction panel.
        /// </summary>
        /// <param name="sender">The initiate transaction panel being initialized.</param>
        private async void InitializeInitiateTransactionPanel(Panel sender)
        {
            var panel = (sender as InitiateTransactionPanel)!;
            switch (this.txMode)
            {
                case TransactionMode.MintERC20
                or TransactionMode.BurnERC20
                or TransactionMode.MintERC1155
                or TransactionMode.BurnERC1155:
                    panel.RecipientField.value = this.account.Address;
                    panel.RecipientField.isReadOnly = true;
                    panel.RecipientField.focusable = false;
                    break;
                default:
                    panel.RecipientField.value = "";
                    panel.RecipientField.isReadOnly = false;
                    panel.RecipientField.focusable = true;
                    break;
            }

            panel.ClearRecipientButton.focusable = this.txMode != TransactionMode.BurnERC20;
            panel.ClearRecipientButton.SetEnabled(this.txMode != TransactionMode.BurnERC20);
            panel.BalanceLabel.text = "TBD";
            panel.AmountField.value = "";
            panel.CurrencyLabel.text = "TBD";
            panel.EstimatedGasLabel.text = "TBD";
            panel.VerifyTransactionButton.SetEnabled(false);
            panel.VerifyTransactionButton.text = this.txMode switch
            {
                TransactionMode.TransferEth
                or TransactionMode.TransferERC20
                or TransactionMode.TransferERC1155
                    => "Send",
                TransactionMode.MintERC20 or TransactionMode.MintERC1155 => "Mint",
                TransactionMode.BurnERC20 or TransactionMode.BurnERC1155 => "Burn",
                _ => $"Unexpected {nameof(TransactionMode)} {this.txMode}.",
            };
            panel.ClearError();

            // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (this.web3 == null || this.account == null)
                return;
            // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

            panel.CurrencyLabel.text = this.txMode switch
            {
                TransactionMode.TransferEth => Config.NativeTokenTicker,
                TransactionMode.MintERC20
                or TransactionMode.TransferERC20
                or TransactionMode.BurnERC20
                    => await TryCallContract(
                        async () => await this.erc20Service.SymbolQueryAsync()
                    ),
                TransactionMode.MintERC1155
                or TransactionMode.TransferERC1155
                or TransactionMode.BurnERC1155
                    => string.Empty,
                _ => $"Unexpected {nameof(TransactionMode)} {this.txMode}.",
            };

            panel.BalanceLabel.text = await TryCallContract(async () =>
            {
                switch (this.txMode)
                {
                    case TransactionMode.TransferEth:
                        return Web3.Convert
                                .FromWei(
                                    (
                                        await this.web3.Eth.GetBalance.SendRequestAsync(
                                            this.account.Address
                                        )
                                    ).Value
                                )
                                .ToString(CultureInfo.InvariantCulture)
                            + " "
                            + Config.NativeTokenTicker;
                    case TransactionMode.MintERC20
                    or TransactionMode.TransferERC20
                    or TransactionMode.BurnERC20:
                        return Web3.Convert
                                .FromWei(
                                    await this.erc20Service.BalanceOfQueryAsync(
                                        this.account.Address
                                    )
                                )
                                .ToString(CultureInfo.InvariantCulture)
                            + " "
                            + panel.CurrencyLabel.text;
                    case TransactionMode.MintERC1155
                    or TransactionMode.TransferERC1155
                    or TransactionMode.BurnERC1155:
                        var balance = await this.erc1155Service.BalanceOfQueryAsync(
                            this.account.Address,
                            21155
                        );
                        return $"{Web3.Convert.FromWei(balance)} * 10^18 ({balance})";
                    default:
                        return $"Unexpected {nameof(TransactionMode)} {this.txMode}.";
                }
            });
        }

        /// <summary>
        /// Set the transaction mode and switches to the initiate transaction panel.
        /// </summary>
        /// <param name="mode">The transaction mode being used.</param>
        private void InitiateTransfer(TransactionMode mode)
        {
            this.txMode = mode;
            this.initiateTransactionPanel.Show();
        }

        /// <summary>
        /// Clear the recipient field.
        /// </summary>
        /// <param name="panel">The initiate transaction panel from which the recipient field is being cleared.</param>
        private void ClearRecipient(InitiateTransactionPanel panel)
        {
            if (this.txMode == TransactionMode.BurnERC20)
                return;

            panel.RecipientField.value = "";
            panel.RecipientField.isReadOnly = false;
            panel.RecipientField.focusable = true;

            if (
                this.txMode
                is TransactionMode.MintERC20
                    or TransactionMode.MintERC1155
                    or TransactionMode.BurnERC1155
            )
            {
                panel.BalanceLabel.text = "TBD";
            }
        }

        /// <summary>
        /// Updates the displayed balance.
        /// </summary>
        /// <param name="panel">The initiate transaction panel from which the balance label is being updated.</param>
        /// <exception cref="InvalidOperationException">Thrown when the set <see cref="txMode"/> has an unexpected
        /// value.
        /// </exception>
        private async Task HandleUpdateFromBalance(InitiateTransactionPanel panel) =>
            panel.BalanceLabel.text = await TryCallContract(async () =>
            {
                switch (this.txMode)
                {
                    case TransactionMode.MintERC20:
                        return Web3.Convert
                                .FromWei(
                                    await this.erc20Service.BalanceOfQueryAsync(
                                        panel.RecipientField.value
                                    )
                                )
                                .ToString(CultureInfo.InvariantCulture)
                            + " "
                            + panel.CurrencyLabel.text;
                    case TransactionMode.MintERC1155
                    or TransactionMode.BurnERC1155:
                        var balance = await this.erc1155Service.BalanceOfQueryAsync(
                            panel.RecipientField.value,
                            21155
                        );
                        return $"{Web3.Convert.FromWei(balance)} * 10^18 ({balance})";
                    default:
                        throw new InvalidOperationException(
                            $"Unexpected {nameof(TransactionMode)} {this.txMode}."
                        );
                }
            });

        /// <summary>
        /// Updates the estimated gas.
        /// </summary>
        /// <param name="panel">The initiate transaction panel from which the estimated gas is being updated.</param>
        /// <exception cref="InvalidOperationException">Thrown when the set <see cref="txMode"/> has an unexpected
        /// value.
        /// </exception>
        private async Task HandleUpdateEstimatedGas(InitiateTransactionPanel panel)
        {
            BigInteger amount,
                estimatedGas = 0,
                gasPrice = 0,
                ethBalance = 0;

            panel.EstimatedGasLabel.text = "TBD";
            panel.ClearError();
            panel.VerifyTransactionButton.SetEnabled(false);

            if (panel.AmountField.value.Trim() == "")
                return;

            try
            {
                var minimumAmount = Web3.Convert.FromWei(
                    1,
                    this.txMode switch
                    {
                        // Ethereum spec
                        TransactionMode.TransferEth
                            => 18,
                        TransactionMode.MintERC20
                        or TransactionMode.TransferERC20
                        or TransactionMode.BurnERC20
                            => await this.erc20Service.DecimalsQueryAsync(),
                        // Can be arbitrary
                        TransactionMode.MintERC1155
                        or TransactionMode.TransferERC1155
                        or TransactionMode.BurnERC1155
                            => 18,
                        _
                            => throw new InvalidOperationException(
                                $"Unexpected {nameof(TransactionMode)} {this.txMode}."
                            ),
                    }
                );
                var rawAmount = BigDecimal.Parse(panel.AmountField.value);
                if (rawAmount < minimumAmount)
                    throw new FormatException();

                amount = Web3.Convert.ToWei(rawAmount);
            }
            catch (FormatException)
            {
                panel.InvalidAmountErrorLabel.style.display = DisplayStyle.Flex;
                return;
            }
            catch (SocketException)
            {
                panel.NetworkErrorLabel.style.display = DisplayStyle.Flex;
                return;
            }
            catch (HttpRequestException)
            {
                panel.NodeErrorLabel.style.display = DisplayStyle.Flex;
                return;
            }

            if (!panel.RecipientField.isReadOnly)
                return;

            try
            {
                estimatedGas = this.txMode switch
                {
                    TransactionMode.TransferEth
                        => await this.ethTransferService.EstimateGasAsync(
                            panel.RecipientField.value,
                            Web3.Convert.FromWei(amount)
                        ),
                    TransactionMode.MintERC20
                        => await this.erc20Service.ContractHandler.EstimateGasAsync(
                            new ERC20TokenDefinition.MintFunction
                            {
                                To = panel.RecipientField.value,
                                Amount = amount,
                            }
                        ),
                    TransactionMode.TransferERC20
                        => await this.erc20Service.ContractHandler.EstimateGasAsync(
                            new ERC20TokenDefinition.TransferFunction
                            {
                                To = panel.RecipientField.value,
                                Amount = amount,
                            }
                        ),
                    TransactionMode.BurnERC20
                        => await this.erc20Service.ContractHandler.EstimateGasAsync(
                            new ERC20TokenDefinition.BurnFunction { Amount = amount }
                        ),
                    TransactionMode.MintERC1155
                        => await this.erc1155Service.ContractHandler.EstimateGasAsync(
                            new ERC1155TokenDefinition.MintFunction
                            {
                                Account = panel.RecipientField.value,
                                Id = 21155,
                                Amount = amount,
                                Data = Array.Empty<byte>(),
                            }
                        ),
                    TransactionMode.TransferERC1155
                        => await this.erc1155Service.ContractHandler.EstimateGasAsync(
                            new ERC1155TokenDefinition.SafeTransferFromFunction
                            {
                                From = this.account.Address,
                                To = panel.RecipientField.value,
                                Id = 21155,
                                Amount = amount,
                                Data = System.Text.Encoding.UTF8.GetBytes("arbitrary data"),
                            }
                        ),
                    TransactionMode.BurnERC1155
                        => await this.erc1155Service.ContractHandler.EstimateGasAsync(
                            new ERC1155TokenDefinition.BurnFunction
                            {
                                Account = panel.RecipientField.value,
                                Id = 21155,
                                Value = amount,
                            }
                        ),
                    _
                        => throw new InvalidOperationException(
                            $"Unexpected {nameof(TransactionMode)} {this.txMode}."
                        ),
                };
                gasPrice = (await this.web3.Eth.GasPrice.SendRequestAsync()).Value;
                ethBalance = (
                    await this.web3.Eth.GetBalance.SendRequestAsync(this.account.Address)
                ).Value;
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case RpcResponseException when e.Message.Contains("insufficient funds"):
                        panel.InsufficientFundsErrorLabel.style.display = DisplayStyle.Flex;
                        return;
                    case RpcResponseException when e.Message.Contains("execution reverted"): // polygon edge
                    case SmartContractRevertException:
                        if (
                            e.Message.Contains("amount exceeds balance")
                            || e.Message.Contains("insufficient balance")
                        )
                        {
                            panel.InsufficientFundsErrorLabel.style.display = DisplayStyle.Flex;
                        }
                        else if (e.Message.Contains("not token owner or approved"))
                        {
                            panel.NotApprovedErrorLabel.style.display = DisplayStyle.Flex;
                        }
                        else if (e.Message.Contains("caller is not the owner"))
                        {
                            panel.NotOwnerErrorLabel.style.display = DisplayStyle.Flex;
                        }
                        else
                            throw;
                        return;
                    case RpcResponseException:
                        throw;
                    case SocketException:
                        panel.NetworkErrorLabel.style.display = DisplayStyle.Flex;
                        return;
                    case HttpRequestException:
                        panel.NodeErrorLabel.style.display = DisplayStyle.Flex;
                        return;
                }
            }

            var estimatedTotalGasValue = estimatedGas * gasPrice;

            panel.EstimatedGasLabel.text =
                Web3.Convert.FromWei(estimatedTotalGasValue).ToString(CultureInfo.InvariantCulture)
                + " "
                + Config.NativeTokenTicker;

            try
            {
                var insufficientFunds = this.txMode switch
                {
                    TransactionMode.TransferEth => amount + estimatedTotalGasValue > ethBalance,
                    TransactionMode.TransferERC20
                        => amount
                            > await this.erc20Service.BalanceOfQueryAsync(this.account.Address),
                    TransactionMode.BurnERC20
                        => amount
                            > await this.erc20Service.BalanceOfQueryAsync(
                                panel.RecipientField.value
                            ),
                    TransactionMode.TransferERC1155
                        => amount
                            > await this.erc1155Service.BalanceOfQueryAsync(
                                this.account.Address,
                                21155
                            ),
                    TransactionMode.BurnERC1155
                        => amount
                            > await this.erc1155Service.BalanceOfQueryAsync(
                                panel.RecipientField.value,
                                21155
                            ),
                    _ => false,
                };

                if (insufficientFunds)
                {
                    panel.InsufficientFundsErrorLabel.style.display = DisplayStyle.Flex;
                    return;
                }

                if (estimatedTotalGasValue > ethBalance)
                {
                    panel.InsufficientGasErrorLabel.style.display = DisplayStyle.Flex;
                    return;
                }

                panel.VerifyTransactionButton.SetEnabled(true);
            }
            catch (SocketException)
            {
                panel.NetworkErrorLabel.style.display = DisplayStyle.Flex;
            }
            catch (HttpRequestException)
            {
                panel.NodeErrorLabel.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// Creates an <see cref="EventCallback{TEventType}"/> event handler that validates the recipient and update
        /// the balance and estimated gas if valid.
        /// </summary>
        /// <param name="panel">The initiate transaction panel from which the recipient is being validated.</param>
        /// <returns>An <see cref="EventCallback{TEventType}"/> used by Unity RegisterCallback method.</returns>
        private EventCallback<ChangeEvent<string>> HandleRecipientFieldChange(
            InitiateTransactionPanel panel
        )
        {
            void Handler(ChangeEvent<string> evt)
            {
                if (
                    // ReSharper disable RedundantNameQualifier
                    Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(evt.newValue)
                    && Nethereum.Util.AddressUtil.Current.IsChecksumAddress(evt.newValue)
                    // ReSharper restore RedundantNameQualifier
                )
                {
                    panel.RecipientField.isReadOnly = true;
                    panel.RecipientField.focusable = false;
                    if (
                        this.txMode
                        is TransactionMode.MintERC20
                            or TransactionMode.MintERC1155
                            or TransactionMode.BurnERC1155
                    )
                        this.UpdateFromBalance.Invoke();

                    this.UpdateEstimatedGas.Invoke();
                    return;
                }

                panel.ClearError();

                if (panel.RecipientField.value == "")
                    return;

                panel.RecipientErrorLabel.style.display = DisplayStyle.Flex;
            }

            return Handler;
        }

        /// <summary>
        /// Creates an <see cref="EventCallback{TEventType}"/> that validates the amount and update the balance and
        /// estimated gas if valid.
        /// </summary>
        /// <returns>An <see cref="EventCallback{TEventType}"/> used by Unity RegisterCallback method.</returns>
        private EventCallback<ChangeEvent<string>> HandleAmountFieldChange()
        {
            DateTimeOffset lastChanged;

            async void Handler(ChangeEvent<string> evt)
            {
                var changed = lastChanged = DateTime.Now;
                await Task.Delay(500);
                if (changed < lastChanged)
                    return;
                this.UpdateEstimatedGas.Invoke();
            }

            return Handler;
        }

        /// <summary>
        /// Verifies the requested transaction.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the set <see cref="txMode"/> has an unexpected
        /// value.
        /// </exception>
        private async void VerifyTransaction()
        {
            this.waitReceiptPanel.Show();
            try
            {
                var receipt = await (
                    this.txMode switch
                    {
                        TransactionMode.TransferEth
                            => this.ethTransferService.TransferEtherAndWaitForReceiptAsync(
                                this.initiateTransactionPanel.RecipientField.value,
                                Convert.ToDecimal(this.initiateTransactionPanel.AmountField.value)
                            ),
                        TransactionMode.MintERC20
                            => this.erc20Service.MintRequestAndWaitForReceiptAsync(
                                this.initiateTransactionPanel.RecipientField.value,
                                Web3.Convert.ToWei(
                                    BigDecimal.Parse(
                                        this.initiateTransactionPanel.AmountField.value
                                    )
                                )
                            ),
                        TransactionMode.TransferERC20
                            => this.erc20Service.TransferRequestAndWaitForReceiptAsync(
                                this.initiateTransactionPanel.RecipientField.value,
                                Web3.Convert.ToWei(
                                    BigDecimal.Parse(
                                        this.initiateTransactionPanel.AmountField.value
                                    )
                                )
                            ),
                        TransactionMode.BurnERC20
                            => this.erc20Service.BurnRequestAndWaitForReceiptAsync(
                                Web3.Convert.ToWei(
                                    BigDecimal.Parse(
                                        this.initiateTransactionPanel.AmountField.value
                                    )
                                )
                            ),
                        TransactionMode.MintERC1155
                            => this.erc1155Service.MintRequestAndWaitForReceiptAsync(
                                this.initiateTransactionPanel.RecipientField.value,
                                21155,
                                Web3.Convert.ToWei(
                                    BigDecimal.Parse(
                                        this.initiateTransactionPanel.AmountField.value
                                    )
                                ),
                                Array.Empty<byte>()
                            ),
                        TransactionMode.TransferERC1155
                            => this.erc1155Service.SafeTransferFromRequestAndWaitForReceiptAsync(
                                this.account.Address,
                                this.initiateTransactionPanel.RecipientField.value,
                                21155,
                                Web3.Convert.ToWei(
                                    BigDecimal.Parse(
                                        this.initiateTransactionPanel.AmountField.value
                                    )
                                ),
                                System.Text.Encoding.UTF8.GetBytes("arbitrary data")
                            ),
                        TransactionMode.BurnERC1155
                            => this.erc1155Service.BurnRequestAndWaitForReceiptAsync(
                                this.initiateTransactionPanel.RecipientField.value,
                                21155,
                                Web3.Convert.ToWei(
                                    BigDecimal.Parse(
                                        this.initiateTransactionPanel.AmountField.value
                                    )
                                )
                            ),
                        _
                            => throw new InvalidOperationException(
                                $"Unexpected {nameof(TransactionMode)} {this.txMode}."
                            ),
                    }
                );

                this.waitReceiptPanel.TransactionIdLabel.text = receipt.TransactionHash;
                this.waitReceiptPanel.BlockNumberLabel.text = receipt.BlockNumber.ToString();
                this.waitReceiptPanel.LoadingLabel.style.display = DisplayStyle.None;
                this.waitReceiptPanel.TransactionInfoContainer.style.display = DisplayStyle.Flex;
                this.waitReceiptPanel.CloseButton.focusable = true;
                this.waitReceiptPanel.CloseButton.SetEnabled(true);
                this.waitReceiptPanel.ShowTransactionButton.focusable = true;
                this.waitReceiptPanel.ShowTransactionButton.SetEnabled(true);
            }
            catch (Exception e)
            {
                this.waitReceiptPanel.LoadingLabel.style.display = DisplayStyle.None;
                this.waitReceiptPanel.ErrorLabel.style.display = DisplayStyle.Flex;
                this.waitReceiptPanel.ErrorLabel.text = e.Message;
                this.waitReceiptPanel.CloseButton.focusable = true;
                this.waitReceiptPanel.CloseButton.SetEnabled(true);

                switch (e)
                {
                    case RpcResponseException when !e.Message.Contains("insufficient funds"):
                        throw;
                    case RpcResponseException when e.Message.Contains("execution was reverted"): // for polygon edge
                    case SmartContractRevertException:
                        if (
                            !e.Message.Contains("amount exceeds balance")
                            || !e.Message.Contains("insufficient balance")
                            || !e.Message.Contains("not token owner or approved")
                            || !e.Message.Contains("caller is not the owner")
                        )
                            throw;
                        return;
                    case SocketException
                    or HttpRequestException:
                        return;
                }
            }
        }

        /// <summary>
        /// Resets the wait receipt panel.
        /// </summary>
        /// <param name="sender">The wait receipt panel being reset.</param>
        private void ResetWaitReceiptPanel(Panel sender)
        {
            var panel = (sender as WaitReceiptPanel)!;
            panel.LoadingLabel.style.display = DisplayStyle.Flex;
            panel.ErrorLabel.style.display = DisplayStyle.None;
            panel.TransactionInfoContainer.style.display = DisplayStyle.None;
            panel.CloseButton.focusable = false;
            panel.CloseButton.SetEnabled(false);
            panel.ShowTransactionButton.focusable = false;
            panel.ShowTransactionButton.SetEnabled(false);
        }

        /// <summary>
        /// Represents the transaction mode.
        /// </summary>
        private enum TransactionMode
        {
            TransferEth,

            // ReSharper disable InconsistentNaming
            MintERC20,
            TransferERC20,
            BurnERC20,
            MintERC1155,
            TransferERC1155,
            BurnERC1155,
            // ReSharper restore InconsistentNaming
        }
    }
}
