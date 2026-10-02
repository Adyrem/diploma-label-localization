namespace BE.LabelExtension.Core.Models
{
    /// <summary>
    /// The rule deciding whether a model is read-only. It is kept here and nowhere else.
    /// </summary>
    /// <remarks>
    /// As in the existing tool, a model is read-only if it is locked or its layer lies
    /// outside VAR to CUP. In addition, everything in the reference directories of the
    /// metadata configuration (FrameworkDirectory and ReferencePackagesPaths) is read-only. A
    /// read-only file attribute is no criterion; if writing fails, the error is reported.
    /// </remarks>
    public static class ModelAccessRule
    {
        /// <summary>First layer that may be written to.</summary>
        public const ModelLayer FirstWritableLayer = ModelLayer.VAR;

        /// <summary>Last layer that may be written to.</summary>
        public const ModelLayer LastWritableLayer = ModelLayer.CUP;

        /// <summary>Decides whether a model is read-only.</summary>
        /// <param name="layer">Layer from the descriptor.</param>
        /// <param name="isLocked">Value of the element Locked, <c>false</c> if it is missing.</param>
        /// <param name="isInReferenceDirectory">Whether the model lies in a reference directory.</param>
        /// <returns>Whether labels of the model must not be changed.</returns>
        public static bool IsReadOnly(ModelLayer layer, bool isLocked, bool isInReferenceDirectory)
            => isInReferenceDirectory
               || isLocked
               || layer < FirstWritableLayer
               || layer > LastWritableLayer;
    }
}
