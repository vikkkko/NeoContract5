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

备注：

Verification 触发器执行时如果有参数需要传参，否则会因为栈不平而失败

Verification 触发器的参数要放在交易的 Witness 的 InvocationScript 字段中，可以用 ScriptBuilder 来构造

Verification 触发器中不能执行下面的代码
var callscript = ExecutionEngine.CallingScriptHash;

未部署的合约不能执行 Storage.Get() 方法

Storage.Get() 如果查询不到的话，返回 byte[0] 而不是 null

Application 触发器中如果调用 CheckWitness() 的话，需要在 TransactionAttribute 中传附加人的签名 Usage = TransactionAttributeUsage.Script Data = ScriptHash，并且在 Scripts 中添加一个新的 Witness
