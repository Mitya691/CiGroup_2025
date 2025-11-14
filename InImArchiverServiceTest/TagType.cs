using System;

namespace InImArchiverService
{
    /// <summary>
    /// Тип переменной, связанной с тегом MySCADA.
    /// </summary>
    public class TagType
    {
        /// <summary>
        /// Идентификатор типа в базе данных.
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// Тип .NET, соовтетствующий типу этого тега в удалённом устройстве.
        /// </summary>
        public Type DotNetType { get; }

        /// <summary>
        /// Описание типа в базе данных.
        /// </summary>
        public string Description { get; }

        public TagType(long id, Type dotNetType, string description)
        {
            Id = id;
            DotNetType = dotNetType;
            Description = description;
        }

        public override string ToString()
        {
            return DotNetType.Name;
        }
    }
}