## Unit test result

Test environment:

private net

neo-cli 2.7.6 (must > 2.7.5)

neo-gui 2.7.6

| Method                   | Description                        | How to test                    | Test Result |
| ------------------------ | ---------------------------------- | ------------------------------ | ----------- |
| balanceOf                | see nep-5                          | neo-gui                        | ✅           |
| decimals                 | see nep-5                          | neo-gui                        | ✅           |
| getRefundTarget          | get who want to refund this UTXO   | neo-gui                        | ✅           |
| getTxInfo                | get tx info                        | neo-gui                        | ✅           |
| mintTokens               | GAS → SGAS                         | c# code + neo-cli              | ✅           |
| name                     | see nep-5                          | neo-gui                        | ✅           |
| refund                   | SGAS → GAS 1/2 step                | c# code + neo-cli              | ✅           |
| symbol                   | see nep-5                          | neo-gui                        | ✅           |
| totalSupply              | see nep-5                          | neo-gui                        | ✅           |
| transfer                 | see nep-5                          | neo-gui or neo-cli             | ✅           |
| transferAPP              | transfer from other smart contract | neo-gui + other smart contract | 👩‍💻          |
| TriggerType.Verification |                                    | c# code + neo-cli              | ✅           |

Useful Test Tools: [https://github.com/chenzhitong/ApplicationLogsTools](https://github.com/chenzhitong/ApplicationLogsTools)

2018/7/29 Testing in progress.
