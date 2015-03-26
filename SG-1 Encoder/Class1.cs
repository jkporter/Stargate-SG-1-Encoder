using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Globalization;

namespace SG_1_Encoder
{
    public class DVD
    {
        private readonly List<Title> m_titles;

        /// <summary>
        /// Default constructor for this object
        /// </summary>
        public DVD()
        {
            m_titles = new List<Title>();
        }

        /// <summary>
        /// Collection of Titles associated with this DVD
        /// </summary>
        public List<Title> Titles
        {
            get { return m_titles; }
        }

        public static DVD Parse(StreamReader output)
        {
            var thisDVD = new DVD();

            while (!output.EndOfStream)
            {
                if ((char)output.Peek() == '+')
                    thisDVD.m_titles.AddRange(Title.ParseList(output.ReadToEnd()));
                else
                    output.ReadLine();
            }

            return thisDVD;
        }
    }

    public class Title
    {
        private static readonly CultureInfo Culture = new CultureInfo("en-US", false);
        private readonly List<AudioTrack> m_audioTracks;
        private readonly List<Chapter> m_chapters;
        private readonly List<Subtitle> m_subtitles;
        private List<String> m_angles = new List<string>();
        private float m_aspectRatio;
        private int[] m_autoCrop;
        private TimeSpan m_duration;
        private Size m_resolution;
        private int m_titleNumber;
        private Size m_parVal;

        /// <summary>
        /// The constructor for this object
        /// </summary>
        public Title()
        {
            m_audioTracks = new List<AudioTrack>();
            m_chapters = new List<Chapter>();
            m_subtitles = new List<Subtitle>();
        }

        /// <summary>
        /// Collection of chapters in this Title
        /// </summary>
        public List<Chapter> Chapters
        {
            get { return m_chapters; }
        }

        /// <summary>
        /// Collection of audio tracks associated with this Title
        /// </summary>
        public List<AudioTrack> AudioTracks
        {
            get { return m_audioTracks; }
        }

        /// <summary>
        /// Collection of subtitles associated with this Title
        /// </summary>
        public List<Subtitle> Subtitles
        {
            get { return m_subtitles; }
        }

        /// <summary>
        /// The track number of this Title
        /// </summary>
        public int TitleNumber
        {
            get { return m_titleNumber; }
        }

        /// <summary>
        /// The length in time of this Title
        /// </summary>
        public TimeSpan Duration
        {
            get { return m_duration; }
        }

        /// <summary>
        /// The resolution (width/height) of this Title
        /// </summary>
        public Size Resolution
        {
            get { return m_resolution; }
        }

        /// <summary>
        /// The aspect ratio of this Title
        /// </summary>
        public float AspectRatio
        {
            get { return m_aspectRatio; }
        }

        /// <summary>
        /// Par Value
        /// </summary>
        public Size ParVal
        {
            get { return m_parVal; }
        }

        /// <summary>
        /// The automatically detected crop region for this Title.
        /// This is an int array with 4 items in it as so:
        /// 0: 
        /// 1: 
        /// 2: 
        /// 3: 
        /// </summary>
        public int[] AutoCropDimensions
        {
            get { return m_autoCrop; }
        }

        /// <summary>
        /// Collection of Angles in this Title
        /// </summary>
        public List<string> Angles
        {
            get { return m_angles; }
        }

        /// <summary>
        /// Override of the ToString method to provide an easy way to use this object in the UI
        /// </summary>
        /// <returns>A string representing this track in the format: {title #} (00:00:00)</returns>
        public override string ToString()
        {
            return string.Format("{0} ({1:00}:{2:00}:{3:00})", m_titleNumber, m_duration.Hours,
                                 m_duration.Minutes, m_duration.Seconds);
        }

        public static Title Parse(StringReader output)
        {
            var thisTitle = new Title();

            Match m = Regex.Match(output.ReadLine(), @"^\+ title ([0-9]*):");
            // Match track number for this title
            if (m.Success)
                thisTitle.m_titleNumber = int.Parse(m.Groups[1].Value.Trim());

            output.ReadLine();

            /* if (!Properties.Settings.Default.noDvdNav)
            { */
                // Get the Angles for the title.
                m = Regex.Match(output.ReadLine(), @"  \+ angle\(s\) ([0-9])");
                if (m.Success)
                {
                    String angleList = m.Value.Replace("+ angle(s) ", "").Trim();
                    int angleCount;
                    int.TryParse(angleList, out angleCount);

                    for (int i = 1; i <= angleCount; i++)
                        thisTitle.m_angles.Add(i.ToString());
                }
            /* }  */

            // Get duration for this title
            m = Regex.Match(output.ReadLine(), @"^  \+ duration: ([0-9]{2}:[0-9]{2}:[0-9]{2})");
            if (m.Success)
                thisTitle.m_duration = TimeSpan.Parse(m.Groups[1].Value);

            // Get resolution, aspect ratio and FPS for this title
            m = Regex.Match(output.ReadLine(),
                            @"^  \+ size: ([0-9]*)x([0-9]*), pixel aspect: ([0-9]*)/([0-9]*), display aspect: ([0-9]*\.[0-9]*), ([0-9]*\.[0-9]*) fps");
            //size: 720x576, pixel aspect: 16/15, display aspect: 1.33, 25.000 fps

            if (m.Success)
            {
                thisTitle.m_resolution = new Size(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
                thisTitle.m_parVal = new Size(int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value));
                thisTitle.m_aspectRatio = float.Parse(m.Groups[5].Value, Culture);
            }

            // Get autocrop region for this title
            m = Regex.Match(output.ReadLine(), @"^  \+ autocrop: ([0-9]*)/([0-9]*)/([0-9]*)/([0-9]*)");
            if (m.Success)
                thisTitle.m_autoCrop = new int[]
                                           {
                                               int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value),
                                               int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value)
                                           };

            thisTitle.m_chapters.AddRange(Chapter.ParseList(output));

            thisTitle.m_audioTracks.AddRange(AudioTrack.ParseList(output));

            thisTitle.m_subtitles.AddRange(Subtitle.ParseList(output));

            return thisTitle;
        }

        public static Title[] ParseList(string output)
        {
            var titles = new List<Title>();
            var sr = new StringReader(output);

            while (sr.Peek() == '+' || sr.Peek() == ' ')
            {
                // If the the character is a space, then chances are the line
                if (sr.Peek() == ' ') // If the character is a space, then chances are it's the combing detected line.
                    sr.ReadLine(); // Skip over it
                else
                    titles.Add(Parse(sr));
            }

            return titles.ToArray();
        }
    }

    public class AudioTrack
    {
        private int m_bitrate;
        private string m_format;
        private int m_frequency;
        private string m_language;
        private string m_subFormat;
        private int m_trackNumber;
        private string m_iso639_2;

        /// <summary>
        /// The track number of this Audio Track
        /// </summary>
        public int TrackNumber
        {
            get { return m_trackNumber; }
        }

        /// <summary>
        /// The language (if detected) of this Audio Track
        /// </summary>
        public string Language
        {
            get { return m_language; }
        }

        /// <summary>
        /// The primary format of this Audio Track
        /// </summary>
        public string Format
        {
            get { return m_format; }
        }

        /// <summary>
        /// Additional format info for this Audio Track
        /// </summary>
        public string SubFormat
        {
            get { return m_subFormat; }
        }

        /// <summary>
        /// The frequency (in MHz) of this Audio Track
        /// </summary>
        public int Frequency
        {
            get { return m_frequency; }
        }

        /// <summary>
        /// The bitrate (in kbps) of this Audio Track
        /// </summary>
        public int Bitrate
        {
            get { return m_bitrate; }
        }

        public string ISO639_2
        {
            get { return m_iso639_2; }
        }

        /// <summary>
        /// Override of the ToString method to make this object easier to use in the UI
        /// </summary>
        /// <returns>A string formatted as: {track #} {language} ({format}) ({sub-format})</returns>
        public override string ToString()
        {
            if (m_subFormat == null)
                return string.Format("{0} {1} ({2})", m_trackNumber, m_language, m_format);

            return string.Format("{0} {1} ({2}) ({3})", m_trackNumber, m_language, m_format, m_subFormat);
        }

        public static AudioTrack Parse(StringReader output)
        {
            String audio_track = output.ReadLine();
            Match m = Regex.Match(audio_track, @"^    \+ ([0-9]*), ([A-Za-z0-9]*) \((.*)\) \((.*)\)");
            Match track = Regex.Match(audio_track, @"^    \+ ([0-9]*), ([A-Za-z0-9]*) \((.*)\)"); // ID and Language
            Match iso639_2 = Regex.Match(audio_track, @"iso639-2: ([a-zA-Z]*)\)");
            Match samplerate = Regex.Match(audio_track, @"([0-9]*)Hz");
            Match bitrate = Regex.Match(audio_track, @"([0-9]*)bps");

            string subformat = m.Groups[4].Value.Trim().Contains("iso639") ? null : m.Groups[4].Value;
            string samplerateVal = samplerate.Success ? samplerate.Groups[0].Value.Replace("Hz", "").Trim() : "0";
            string bitrateVal = bitrate.Success ? bitrate.Groups[0].Value.Replace("bps", "").Trim() : "0";

            if (track.Success)
            {
                var thisTrack = new AudioTrack
                {
                    m_trackNumber = int.Parse(track.Groups[1].Value.Trim()),
                    m_language = track.Groups[2].Value,
                    m_format = m.Groups[3].Value,
                    m_subFormat = subformat,
                    m_frequency = int.Parse(samplerateVal),
                    m_bitrate = int.Parse(bitrateVal),
                    m_iso639_2 = iso639_2.Value.Replace("iso639-2: ", "").Replace(")", "")
                };
                return thisTrack;
            }

            return null;
        }

        public static AudioTrack[] ParseList(StringReader output)
        {
            var tracks = new List<AudioTrack>();
            while (true)
            {
                AudioTrack thisTrack = Parse(output);
                if (thisTrack != null)
                    tracks.Add(thisTrack);
                else
                    break;
            }
            return tracks.ToArray();
        }
    }

    public class Chapter
    {
        private int m_chapterNumber;

        private TimeSpan m_duration;

        /// <summary>
        /// The number of this Chapter, in regards to it's parent Title
        /// </summary>
        public int ChapterNumber
        {
            get { return m_chapterNumber; }
        }

        /// <summary>
        /// The length in time this Chapter spans
        /// </summary>
        public TimeSpan Duration
        {
            get { return m_duration; }
        }

        /// <summary>
        /// Override of the ToString method to make this object easier to use in the UI
        /// </summary>
        /// <returns>A string formatted as: {chapter #}</returns>
        public override string ToString()
        {
            return m_chapterNumber.ToString();
        }

        public static Chapter Parse(StringReader output)
        {
            Match m = Regex.Match(output.ReadLine(),
                                  @"^    \+ ([0-9]*): cells ([0-9]*)->([0-9]*), ([0-9]*) blocks, duration ([0-9]{2}:[0-9]{2}:[0-9]{2})");
            if (m.Success)
            {
                var thisChapter = new Chapter
                {
                    m_chapterNumber = int.Parse(m.Groups[1].Value.Trim()),
                    m_duration = TimeSpan.Parse(m.Groups[5].Value)
                };
                return thisChapter;
            }
            return null;
        }

        public static Chapter[] ParseList(StringReader output)
        {
            var chapters = new List<Chapter>();

            // this is to read the "  + chapters:" line from the buffer
            // so we can start reading the chapters themselvs
            output.ReadLine();

            while (true)
            {
                // Start of the chapter list for this Title
                Chapter thisChapter = Parse(output);

                if (thisChapter != null)
                    chapters.Add(thisChapter);
                else
                    break;
            }
            return chapters.ToArray();
        }

        
    }
    
    public class Subtitle
        {
            private string m_language;
            private int m_trackNumber;
            private string m_type;
            private string m_typecode;

            /// <summary>
            /// The track number of this Subtitle
            /// </summary>
            public int TrackNumber
            {
                get { return m_trackNumber; }
            }

            /// <summary>
            /// The language (if detected) of this Subtitle
            /// </summary>
            public string Language
            {
                get { return m_language; }
            }

            /// <summary>
            /// Langauage Code
            /// </summary>
            public string LanguageCode
            {
                get { return m_typecode; }
            }


            /// <summary>
            /// Subtitle Type
            /// </summary>
            public string Type
            {
                get { return m_type; }
            }


            /// <summary>
            /// Override of the ToString method to make this object easier to use in the UI
            /// </summary>
            /// <returns>A string formatted as: {track #} {language}</returns>
            public override string ToString()
            {
                return string.Format("{0} {1} ({2})", m_trackNumber, m_language, m_type);
            }

            public static Subtitle Parse(StringReader output)
            {
                string curLine = output.ReadLine();

                Match m = Regex.Match(curLine, @"^    \+ ([0-9]*), ([A-Za-z, ]*) \((.*)\) \(([a-zA-Z]*)\)");
                if (m.Success && !curLine.Contains("HandBrake has exited."))
                {
                    var thisSubtitle = new Subtitle
                    {
                        m_trackNumber = int.Parse(m.Groups[1].Value.Trim()),
                        m_language = m.Groups[2].Value,
                        m_typecode = m.Groups[3].Value,
                        m_type = m.Groups[4].Value
                    };
                    return thisSubtitle;
                }
                return null;
            }

            public static Subtitle[] ParseList(StringReader output)
            {
                var subtitles = new List<Subtitle>();
                while ((char)output.Peek() != '+')
                {
                    Subtitle thisSubtitle = Parse(output);

                    if (thisSubtitle != null)
                        subtitles.Add(thisSubtitle);
                    else
                        break;
                }
                return subtitles.ToArray();
            }
        }
}
