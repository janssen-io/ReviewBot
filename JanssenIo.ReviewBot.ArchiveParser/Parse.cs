using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;

namespace JanssenIo.ReviewBot.ArchiveParser
{
    internal static class Parse
    {
        private static readonly EventId IncorrectFormatId = new EventId(2001, "Incorrect Format");
        private static readonly EventId UnexpectedErrorId = new EventId(2002, "Unexpected Error");
        private static readonly EventId FinishedId = new EventId(2000, "Finished Parsing");

        internal interface IParseArchives
        {
            IEnumerable<Review> Parse(Stream csv);
        }

        internal class GoogleSheetsParser : IParseArchives
        {
            private readonly ILogger<IParseArchives> logger;

            public GoogleSheetsParser(ILogger<IParseArchives> logger)
            {
                this.logger = logger;
            }

            public IEnumerable<Review> Parse(Stream csv)
            {
                using var reader = new StreamReader(csv);
                using var parser = new CsvParser(
                    reader, new CsvConfiguration(CultureInfo.GetCultureInfo("us-US"))
                    {
                        HasHeaderRecord = true,

                    });
                using var csvReader = new CsvReader(parser);
                csvReader.Context.RegisterClassMap<ReviewMapper>();

                csvReader.Read(); // skip header
                csvReader.ReadHeader();

                int numRows = 0;
                int numErrors = 0;
                while(csvReader.Read())
                {
                    Review? review = null;

                    try 
                    { 
                        review = csvReader.GetRecord<Review>();
                        numRows++;
                    }
                    catch (Exception e) 
                    when (e is FormatException || e.InnerException is FormatException)
                    {
                        logger.LogError(IncorrectFormatId, e, e.Message);
                        numErrors++;
                        continue;
                    }
                    catch (Exception e) 
                    {
                        logger.LogCritical(UnexpectedErrorId, e, e.Message); 
                        continue;
                    }

                    if (review != null)
                        yield return review;
                }

                logger.LogInformation(FinishedId, "Finished parsing archive. {Successes} parsed, {Errors} errors", numRows, numErrors);
            }
        }

        private class ReviewMapper : ClassMap<Review> {
#pragma warning disable S1144 // Unused private types or members should be removed
            public ReviewMapper()
            {
                var us = CultureInfo.GetCultureInfo("us-US");
                var nl = CultureInfo.GetCultureInfo("nl-NL");
                string[] formats = us.DateTimeFormat.GetAllDateTimePatterns()
                    .Union(nl.DateTimeFormat.GetAllDateTimePatterns())
                    .ToArray();

                Map(m => m.PublishedOn).Index(7)
                  .TypeConverterOption.Format(formats)
                  .TypeConverterOption.CultureInfo(us);
                Map(m => m.Bottle).Index(1);
                Map(m => m.Author).Index(2);
                Map(m => m.Link).Index(3);
                Map(m => m.Score).Index(4);
                Map(m => m.Region).Index(5);
            }
#pragma warning restore S1144 // Unused private types or members should be removed
        }
    }
}
