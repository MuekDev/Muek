using System.Collections.Generic;
using Muek.Views;
using NAudio.Midi;

namespace Muek.Services;

public class MidiService
{
    public MidiEventCollection Data = new MidiEventCollection(1,480);
    
    public MidiService()
    {
        Data.AddTrack();
        Data.AddEvent(new MetaEvent(MetaEventType.EndTrack,0,0),0);
    }

    public void AddNote(int time, int noteNumber, int length, int velocity = 100, int channel = 1)
    {
        Data.AddEvent(new NoteEvent(time,channel,MidiCommandCode.NoteOn,noteNumber,velocity),1);
        Data.AddEvent(new NoteEvent(time+length,channel,MidiCommandCode.NoteOff,noteNumber,velocity),1);
    }
    
    public void ClearNotes()
    {
        Data.RemoveTrack(1);
        Data.AddTrack();
    }

    public void ImportMidi(string filename)
    {
        var fileData = new MidiFile(filename);
        Data = new MidiEventCollection(1, fileData.DeltaTicksPerQuarterNote);
        for (int i = 1; i < Data.Tracks; i++)
        {
            Data.RemoveTrack(i);
        }
        for (int i = 0; i < fileData.Tracks; i++)
        {
            Data.AddTrack();
            foreach (var e in fileData.Events[i])
            {
                if(e is NoteEvent)
                    Data.AddEvent(e,1);
            }
            Data.AddEvent(new MetaEvent(MetaEventType.EndTrack,0,0),i);
        }
    }

    public void ExportMidi(string filename)
    {
        MidiFile.Export(filename,Data);
    }

    public void PlayNote(int noteNumber, int velocity)
    {
        //TODO
    }
}