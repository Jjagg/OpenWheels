namespace OpenWheels.Rendering
{
    /// <summary>
    /// Information for rendering a batch.
    /// </summary>
    public struct BatchInfo
    {
        /// <summary>
        /// Graphics state to set before rendering the batch.
        /// </summary>
        public readonly GraphicsState GraphicsState;

        /// <summary>
        /// Starting index in the index buffer for the batch.
        /// </summary>
        public readonly int Startindex;

        /// <summary>
        /// Number of indices in the batch.
        /// </summary>
        public readonly int IndexCount;

        /// <summary>
        /// User data attached to the batch.
        /// </summary>
        public readonly object UserData;

        /// <summary>
        /// Create a new BatchInfo instance.
        /// </summary>
        /// <param name="graphicsState">Graphics state to set before rendering the batch.</param>
        /// <param name="startIndex">Starting index in the index buffer for the batch.</param>
        /// <param name="indexCount">Number of indices in the batch.</param>
        /// <param name="userData">User data attached to the batch.</param>
        public BatchInfo(GraphicsState graphicsState, int startindex, int indexCount, object userData)
        {
            GraphicsState = graphicsState;
            Startindex = startindex;
            IndexCount = indexCount;
            UserData = userData;
        }
    }
}
