# BitSerialization library

> **WARNING**: This code is experimental. Use at your own risk.

A .Net data serialization/deserialization library that assits with communicating with hardware that uses a binary protocol.

## Overview

When communicating with certain types hardware, usually over some kind of serial port, it is not uncommon for the hardware's API to be specified as message frames where the message's payload is made up of a sequence of integers of various sizes.

This library allows the message payload's structure to be specified as a `class` or `struct` and handles the the serialization and deserialization automagically using reflection.

The following types can be used within a message format:

  1. Integer types (e.g. `byte`, `int`, etc.)
  2. Enum types.
  3. Other `struct` and `class` types.
  4. Arrays of #1, #2 and #3.

## Why not use .Net's inbuilt data marshalling (p/invoke)?

.Net's data marshalling API (e.g. the [Marshal class](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal)) is intended to be used when communicating with C-style APIs. This includes correctly handling padding between variables to ensure correct type alignment. In addition, it will only ever use the system's endianess when reading and writing integers. However, hardware APIs generally don't include alignment padding to save space and usually require a specific endianness to be used.

## Three implementations

This library includes three different implementations.

  1. **On the fly**: This implementation looks up the type information using reflection every time a serialization or derserialization is requested. This is the simplest implementation. Though it is also the slowest.

  2. **Precalculated**: This implementation looks up the type information using reflection a single time and then stores the steps required to serialize and deserialize the type. This implementation is rather complex (due to its "clever" use of generics). But it is substantially faster than the 'on the fly' implementation.

  3. **Source generators**: This implementation uses the new [C# source generators](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/) feature that is currently available in the preview version of .Net. Its implementation is slightly more complicated than the 'on the fly' implementation. Though it is substantially faster than both the 'precalculated' and 'on the fly' implementations.