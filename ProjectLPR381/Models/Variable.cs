using System;

namespace LinearProgrammingProject.Models
{
    /// <summary>
    /// Represents a decision variable in a Linear Programming model
    /// </summary>
    public class Variable
    {
        public string Name { get; set; }
        public VariableType Type { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public double CurrentValue { get; set; }

        /// <summary>
        /// Creates a new variable with default settings
        /// </summary>
        public Variable(string name, VariableType type = VariableType.Continuous)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
            CurrentValue = 0.0;

            // Set bounds based on variable type
            switch (type)
            {
                case VariableType.Continuous:
                case VariableType.Integer:
                    LowerBound = 0.0; // Non-negative by default
                    UpperBound = double.PositiveInfinity;
                    break;
                case VariableType.Binary:
                    LowerBound = 0.0;
                    UpperBound = 1.0;
                    break;
                case VariableType.Unrestricted:
                    LowerBound = double.NegativeInfinity;
                    UpperBound = double.PositiveInfinity;
                    break;
            }
        }

        /// <summary>
        /// Creates a variable with custom bounds
        /// </summary>
        public Variable(string name, VariableType type, double lowerBound, double upperBound)
            : this(name, type)
        {
            LowerBound = lowerBound;
            UpperBound = upperBound;
        }

        /// <summary>
        /// Checks if the current value is valid for this variable type
        /// </summary>
        public bool IsValueValid(double value)
        {
            // Check bounds
            if (value < LowerBound || value > UpperBound)
                return false;

            // Check type-specific constraints
            switch (Type)
            {
                case VariableType.Integer:
                    return Math.Abs(value - Math.Round(value)) < 1e-9; // Is essentially an integer
                case VariableType.Binary:
                    return Math.Abs(value) < 1e-9 || Math.Abs(value - 1.0) < 1e-9; // Is 0 or 1
                default:
                    return true;
            }
        }

        /// <summary>
        /// Sets the variable value if valid
        /// </summary>
        public bool SetValue(double value)
        {
            if (IsValueValid(value))
            {
                CurrentValue = value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates a string representation of the variable bounds
        /// </summary>
        public string GetBoundsString()
        {
            switch (Type)
            {
                case VariableType.Binary:
                    return $"{Name} ∈ {{0, 1}}";
                case VariableType.Integer:
                    if (double.IsNegativeInfinity(LowerBound) && double.IsPositiveInfinity(UpperBound))
                        return $"{Name} ∈ Z";
                    else if (LowerBound >= 0 && double.IsPositiveInfinity(UpperBound))
                        return $"{Name} ∈ Z+";
                    else
                        return $"{Name} ∈ Z, {LowerBound} ≤ {Name} ≤ {UpperBound}";
                case VariableType.Unrestricted:
                    return $"{Name} unrestricted";
                case VariableType.Continuous:
                default:
                    if (LowerBound >= 0 && double.IsPositiveInfinity(UpperBound))
                        return $"{Name} ≥ 0";
                    else if (double.IsNegativeInfinity(LowerBound) && double.IsPositiveInfinity(UpperBound))
                        return $"{Name} unrestricted";
                    else
                        return $"{LowerBound} ≤ {Name} ≤ {UpperBound}";
            }
        }

        /// <summary>
        /// Creates a deep copy of this variable
        /// </summary>
        public Variable Clone()
        {
            return new Variable(Name, Type, LowerBound, UpperBound)
            {
                CurrentValue = this.CurrentValue
            };
        }

        /// <summary>
        /// String representation of the variable
        /// </summary>
        public override string ToString()
        {
            return $"{Name} ({Type}): {CurrentValue:F3} [{GetBoundsString()}]";
        }

        /// <summary>
        /// Checks equality based on name and type
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Variable other)
            {
                return Name.Equals(other.Name) && Type == other.Type;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Type);
        }
    }

    /// <summary>
    /// Types of decision variables
    /// </summary>
    public enum VariableType
    {
        /// <summary>
        /// Continuous variable (can take any real value within bounds)
        /// </summary>
        Continuous,

        /// <summary>
        /// Integer variable (must be a whole number)
        /// </summary>
        Integer,

        /// <summary>
        /// Binary variable (can only be 0 or 1)
        /// </summary>
        Binary,

        /// <summary>
        /// Unrestricted variable (can be negative, positive, or zero)
        /// </summary>
        Unrestricted
    }
}