namespace PlunkAndPlunder.Construction
{
    /// <summary>
    /// Result of a construction command execution
    /// </summary>
    public struct ConstructionResult
    {
        public bool success;
        public string reason;  // Error message if failed
        public string jobId;   // Job ID if successful

        public static ConstructionResult Success(string jobId = null)
        {
            return new ConstructionResult
            {
                success = true,
                jobId = jobId
            };
        }

        public static ConstructionResult Failure(string reason)
        {
            return new ConstructionResult
            {
                success = false,
                reason = reason
            };
        }
    }

    /// <summary>
    /// Result of validation checks
    /// </summary>
    public struct ValidationResult
    {
        public bool isValid;
        public string reason;

        public static ValidationResult Valid()
        {
            return new ValidationResult { isValid = true };
        }

        public static ValidationResult Invalid(string reason)
        {
            return new ValidationResult { isValid = false, reason = reason };
        }
    }
}
