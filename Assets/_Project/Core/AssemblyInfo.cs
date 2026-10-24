using System.Runtime.CompilerServices;

// 让 EditMode 测试可以直接构造/校验内部状态（耐久的消耗、容器的挂载等），
// 否则这些规则只能通过一整套战斗流程间接验证，成本远高于收益。
[assembly: InternalsVisibleTo("BitRunner.Tests.EditMode")]
