# SRAFC
Three command-line tools (1 per game) to convert between JSON and .bytes files for Harebrained Schemes' Shadowrun games.

## About the command-line tools
This solution includes three projects, each producing a commandline tool targeting one of the Harebrained Schemes (HBS) games. The projects are:
	- srrafc - Shadowrun Returns Asset File Converter
	- dfdcafc - Shadowrun Dragonfall Director's Cut Asset File Converter
	- srhkafc - Shadowrun Hong Kong Asset File Converter

The purpose of these tools is to allow conversion between human-readable JSON and .bytes files for the games. Specifically, it avoids needing to
dip into the editor (which can be a bit sluggish) in order to generate certain types of files for your content packs. If you are working with Unity
asset files, that is an entirely different matter.

Currently supported files (by .bytes extension):

* ab.bytes (AbilityDef)
* ai.bytes (ObjectiveArchetype)
* ambi.bytes (AmbienceTemplate)
* blib.bytes (BackerPCLibrary)
* ch_inst.bytes (CharacterInstance)
* ch_sht.bytes (Character)
* convo.bytes (Conversation)
* cpack.bytes (ProjectDef)
* credits.bytes (It's just a string)
* cvf.bytes (CharacterVariant; DFDC and SRHK only, SRR lacks the CharacterVariant class)
* eq_sht.bytes (EquipmentSheet)
* hiring.bytes (HiringSet)
* item.bytes (ItemDef)
* mf.bytes (Manifest; I see the files but I'm not sure they are used?)
* mode.bytes (ModeDef)
* pb.bytes (PropDef)
* pcode.bytes (PortraitCodeList)
* pl.bytes (PortraitList)
* srm.bytes (MapDef)
* srt.bytes (SceneDef)
* story.bytes (StoryDef)
* submix.bytes (SubMixGroup)
* tml.bytes (TotemList)
* topic.bytes (Topic)

## Why JSON?
HBS used an (obviously) older version of [protobuf-net](https://github.com/protobuf-net/protobuf-net), a .NET implementation
of an (obviously) older version of Google 's Protocol Buffers [protobuf](https://github.com/protocolbuffers/protobuf) to serialize their data.

The typical implementation of protobuf involves intermediate files that describe how to serialize each type of data; this correlates to each of the
various data files that end in '*.bytes'.

There is already the .txt format for files used by the editor; these take advantage of the editor using what I believe is the C++ version of protobuf.
That version has a conversion between the .bytes and .txt formats. The C# version never got that because as they pointed out, a JSON serializer was
now already available so folks would prefer to just use that.

Which may be true but isn't helpful for the editor. Nor for me wanting to write a simple command-line converter in C#. So I chose JSON since I
could convert to that and there are plenty of tools to aid in JSON editing.

## How to use the tools
Each will be bundled with some wrapper scripts; you'll need to set some environment variables to point to the game folder. The script will attempt
to copy a couple of DLLs into a subfolder so that the tool can find them. Aside from that, you could also just copy the tools into the game's
Managed folder alongside all of the game DLLs and it would find them, too. I just dislike giving instructions that involve folks modifying
their game folder needlessly. Still, it's an option.

For the most part, you'll just use the -i and -o options to specify your input and output files. If you stick with the typical naming
convention e.g. eq_sht.bytes for an equipment sheet, .pl.bytes for a portrait list, it will make an attempt to deduce which type of
conversion you are attempting. If it can't figure it out, you can specify the type with the -y option.

If you specify a folder it will attempt all files in the folder and the target must be a folder. The -y option will be required.

