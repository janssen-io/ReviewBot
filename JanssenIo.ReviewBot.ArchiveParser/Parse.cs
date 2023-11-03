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
    public static class Parse
    {
        private static readonly EventId IncorrectFormatId = new EventId(2001, "Incorrect Format");
        private static readonly EventId UnexpectedErrorId = new EventId(2002, "Unexpected Error");
        private static readonly EventId FinishedId = new EventId(2000, "Finished Parsing");

        public interface IParseArchives
        {
            IEnumerable<Review> Parse(Stream csv);
        }

        public class GoogleSheetsParser : IParseArchives
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

                // reading header requires a .Read first
                csvReader.Read();
                csvReader.ReadHeader();

                int numRows = 0;
                int numErrors = 0;
                while (csvReader.Read())
                {
                    Review? review;
                    try
                    {
                        review = csvReader.GetRecord<Review>();
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
                    {
                        logger.LogDebug("Success {Author} {Bottle}", review.Author, review.Bottle);
                        numRows++;
                        yield return review;
                    }
                }

                logger.LogInformation(FinishedId, "Finished parsing archive. {Successes} parsed, {Errors} errors", numRows, numErrors);
            }
        }

        public class ReviewMapper : ClassMap<Review>
        {
            public ReviewMapper()
            {
                var us = CultureInfo.GetCultureInfo("us-US");
                var nl = CultureInfo.GetCultureInfo("nl-NL");
                string[] formats =
                    new[]
                    {
                        "MM/dd/yy", "M/d/yy",
                        "MM-dd-yy", "M-d-yy",
                        "MM.dd.yy", "M.d.yy",
                        "dd/MM/yy", "d/M/yy",
                        "dd-MM-yy", "d-MM-yy",
                        "dd.MM.yy", "d.MM.yy",
                        "M/d/yyyy H:m:s",
                        "MM/dd/yyyy H:m:s",
                        "M/d/yyyy HH:mm:ss",
                        "MM/dd/yyyy HH:mm:ss",
                    }
                    .Union(us.DateTimeFormat.GetAllDateTimePatterns())
                    .Union(nl.DateTimeFormat.GetAllDateTimePatterns())
                    .ToArray();

                Map(m => m.SubmittedOn).Index(0)
                  .TypeConverterOption.Format(formats)
                  .TypeConverterOption.CultureInfo(us);
                Map(m => m.PublishedOn).Index(7)
                  .TypeConverterOption.Format(formats)
                  .TypeConverterOption.CultureInfo(us);
                Map(m => m.Bottle).Index(1);
                Map(m => m.Author).Index(2);
                Map(m => m.Link).Index(3);
                Map(m => m.Score).Index(4);
                Map(m => m.Region).Index(5);
            }
        }
    }
}
