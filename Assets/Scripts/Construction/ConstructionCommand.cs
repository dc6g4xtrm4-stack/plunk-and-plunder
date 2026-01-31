using PlunkAndPlunder.Core;

namespace PlunkAndPlunder.Construction
{
    /// <summary>
    /// Base class for all construction commands
    /// Commands are atomic operations that either succeed completely or fail with no side effects
    /// </summary>
    public abstract class ConstructionCommand
    {
        /// <summary>
        /// Execute the command
        /// Should be atomic - either succeeds completely or fails with no changes
        /// </summary>
        public abstract ConstructionResult Execute(ConstructionState constructionState, GameState gameState);

        /// <summary>
        /// Generate a unique job ID
        /// </summary>
        protected string GenerateJobId()
        {
            return $"job_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        }
    }
}
