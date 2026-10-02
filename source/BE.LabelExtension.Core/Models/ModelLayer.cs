namespace BE.LabelExtension.Core.Models
{
    /// <summary>
    /// Layers of the models, as numbers in the element Layer of the descriptor, from the
    /// solution provider up to the user.
    /// </summary>
    public enum ModelLayer
    {
        /// <summary>System.</summary>
        SYS = 0,

        /// <summary>System patch.</summary>
        SYP = 1,

        /// <summary>Global solution.</summary>
        GLS = 2,

        /// <summary>Global solution patch.</summary>
        GLP = 3,

        /// <summary>Feature pack.</summary>
        FPK = 4,

        /// <summary>Feature pack patch.</summary>
        FPP = 5,

        /// <summary>Solution.</summary>
        SLN = 6,

        /// <summary>Solution patch.</summary>
        SLP = 7,

        /// <summary>Independent software vendor.</summary>
        ISV = 8,

        /// <summary>Independent software vendor patch.</summary>
        ISP = 9,

        /// <summary>Value-added reseller, the partner.</summary>
        VAR = 10,

        /// <summary>Value-added reseller patch.</summary>
        VAP = 11,

        /// <summary>Customer.</summary>
        CUS = 12,

        /// <summary>Customer patch.</summary>
        CUP = 13,

        /// <summary>User.</summary>
        USR = 14,

        /// <summary>User patch.</summary>
        USP = 15,
    }
}
