using Amethyst.SymbolGenerator.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public static class AnnotationProcessor
    {
        private static readonly Dictionary<string, IAnnotationHandler> Handlers = new();
        public static IReadOnlyDictionary<string, IAnnotationHandler> RegisteredHandlers => Handlers;

        static AnnotationProcessor()
        {
            var handlerTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsInterface && !type.IsAbstract)
                .Where(type => type.GetCustomAttribute<AnnotationHandlerAttribute>() is not null)
                .Where(type => typeof(IAnnotationHandler).IsAssignableFrom(type));
            foreach (var handlerType in handlerTypes)
            {
                var attribute = handlerType.GetCustomAttribute<AnnotationHandlerAttribute>();
                if (attribute is null)
                    continue;
                if (Handlers.ContainsKey(attribute.HandlesTag))
                    throw new InvalidOperationException($"Multiple handlers registered for tag '{attribute.HandlesTag}'.");
                if (Activator.CreateInstance(handlerType) is not IAnnotationHandler handlerInstance)
                    throw new InvalidOperationException($"Failed to create instance of handler type '{handlerType.FullName}'.");
                Handlers[attribute.HandlesTag] = handlerInstance;
            }
        }

        public static IEnumerable<ProcessedAnnotation> ProcessAnnotations(IEnumerable<RawAnnotation> annotations)
        {
            foreach (var annotation in annotations)
            {
                if (ProcessAnnotation(annotation) is { } processed)
                    yield return processed;
            }
        }

        public static ProcessedAnnotation? ProcessAnnotation(RawAnnotation annotation)
        {
            if (Handlers.TryGetValue(annotation.Tag, out var handler))
            {
                try
                {
                    var result = handler.Handle(annotation);
                    if (result is null)
                        return null;
                    var processed = new ProcessedAnnotation(annotation, result);
                    Logger.Info($"Processed annotation: {processed.Annotation} for {processed.Target.FullName}");
                    return processed;
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Error processing annotation with tag '{annotation.Tag}' for '{annotation.Target.FullName}': \n    {ex.Message}\n    at {annotation.Location}");
                }
            }
            else
            {
                //Logger.Warning($"No handler registered for annotation tag '{annotation.Tag}'.");
            }
            return null;
        }
    }
}
