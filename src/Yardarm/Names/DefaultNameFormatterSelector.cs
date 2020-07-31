﻿namespace Yardarm.Names
{
    public class DefaultNameFormatterSelector : INameFormatterSelector
    {
        public virtual INameFormatter GetFormatter(NameKind nameKind) => nameKind switch
        {
            NameKind.Class => PascalCaseNameFormatter.Instance,
            NameKind.Property => PascalCaseNameFormatter.Instance,
            NameKind.Enum => PascalCaseNameFormatter.Instance,
            NameKind.EnumMember => PascalCaseNameFormatter.Instance,
            _ => PascalCaseNameFormatter.Instance
        };
    }
}
