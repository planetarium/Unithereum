namespace Unithereum.Samples
{
    /// <summary>
    /// Class containing EVM/JSON-RPC compatible network parameters.
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// The ticker of the native token in EVM.
        /// </summary>
        internal const string NativeTokenTicker = "GoerliETH";

        /// <summary>
        /// JSON-RPC Endpoint URL.
        /// </summary>
        internal const string JsonRpcEndpoint = "https://rpc.sepolia.org";

        /// <summary>
        /// URL template for a transaction in block explorer.
        /// </summary>
        internal const string BlockExplorerTxTemplate = "https://sepolia.etherscan.io//tx/{0}";

        /// <summary>
        /// Contract address for an OpenZeppelin ERC20 token.
        /// </summary>
        internal const string ERC20Address = "";

        /// <summary>
        /// Contract address for an OpenZeppelin ERC1155 token.
        /// </summary>
        internal const string ERC1155Address = "";
    }
}
