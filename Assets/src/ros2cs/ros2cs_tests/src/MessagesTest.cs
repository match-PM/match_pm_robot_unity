﻿// Copyright 2019 Dyno Robotics (by Samuel Lindgren samuel@dynorobotics.se)
// Copyright 2019-2021 Robotec.ai
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using NUnit.Framework;

namespace ROS2.Test
{
    [TestFixture]
    public class MessagesTest
    {
        [Test]
        public void CreateMessage()
        {
            std_msgs.msg.Bool msg = new std_msgs.msg.Bool();
        }

        [Test]
        public void SetBoolData()
        {
            std_msgs.msg.Bool msg = new std_msgs.msg.Bool();
            Assert.That(msg.Data, Is.False);
            msg.Data = true;
            Assert.That(msg.Data, Is.True);
            msg.Data = false;
            Assert.That(msg.Data, Is.False);
        }

        [Test]
        public void SetInt64Data()
        {
            std_msgs.msg.Int64 msg = new std_msgs.msg.Int64();
            Assert.That(msg.Data, Is.EqualTo(0));
            msg.Data = 12345;
            Assert.That(msg.Data, Is.EqualTo(12345));
        }

        [Test]
        public void SetStringData()
        {
            std_msgs.msg.String msg = new std_msgs.msg.String();
            Assert.That(msg.Data, Is.EqualTo(""));
            msg.Data = "Show me what you got!";
            Assert.That(msg.Data, Is.EqualTo("Show me what you got!"));
        }

        [Test]
        public void SetDefaults()
        {
            test_msgs.msg.Defaults msg = new test_msgs.msg.Defaults();
            msg.Int32_value = 24;
            Assert.That(msg.Int32_value, Is.EqualTo(24));
            msg.Float32_value = 3.14F;
            Assert.That(msg.Float32_value, Is.EqualTo(3.14F));
        }

        [Test]
        public void SetStrings()
        {
            test_msgs.msg.Strings msg = new test_msgs.msg.Strings();
            msg.String_value = "Turtles all the way down";
            Assert.That(msg.String_value, Is.EqualTo("Turtles all the way down"));
        }
        [Test]
        public void SetUnboundedSequenses()
        {
            test_msgs.msg.UnboundedSequences msg = new test_msgs.msg.UnboundedSequences();
            bool[] setBoolSequence = new bool[2];
            setBoolSequence[0] = true;
            setBoolSequence[1] = false;
            msg.Bool_values = setBoolSequence;

            bool[] getBoolSequence = msg.Bool_values;
            Assert.That(getBoolSequence.Length, Is.EqualTo(2));
            Assert.That(getBoolSequence[0], Is.True);
            Assert.That(getBoolSequence[1], Is.False);

            int[] setIntSequence = new int[2];
            setIntSequence[0] = 123;
            setIntSequence[1] = 456;
            test_msgs.msg.UnboundedSequences msg2 = new test_msgs.msg.UnboundedSequences();
            msg2.Int32_values = setIntSequence;
            int[] getIntList = msg2.Int32_values;
            Assert.That(getIntList.Length, Is.EqualTo(2));
            Assert.That(getIntList[0], Is.EqualTo(123));
            Assert.That(getIntList[1], Is.EqualTo(456));

            string[] setStringSequence = new string[2];
            setStringSequence[0] = "Hello";
            setStringSequence[1] = "world";
            test_msgs.msg.UnboundedSequences msg3 = new test_msgs.msg.UnboundedSequences();
            msg3.String_values = setStringSequence;
            string[] getStringSequence = msg3.String_values;
            Assert.That(getStringSequence.Length, Is.EqualTo(2));
            Assert.That(getStringSequence[0], Is.EqualTo("Hello"));
            Assert.That(getStringSequence[1], Is.EqualTo("world"));
        }

        [Test]
        public void SetBoundedSequenses()
        {
            test_msgs.msg.BoundedSequences msg = new test_msgs.msg.BoundedSequences();
            bool[] setBoolSequence = new bool[2];
            setBoolSequence[0] = true;
            setBoolSequence[1] = false;
            msg.Bool_values = setBoolSequence;

            bool[] getBoolSequence = msg.Bool_values;
            Assert.That(getBoolSequence.Length, Is.EqualTo(2));
            Assert.That(getBoolSequence[0], Is.True);
            Assert.That(getBoolSequence[1], Is.False);

            int[] setIntSequence = new int[2];
            setIntSequence[0] = 123;
            setIntSequence[1] = 456;
            test_msgs.msg.BoundedSequences msg2 = new test_msgs.msg.BoundedSequences();
            msg2.Int32_values = setIntSequence;
            int[] getIntList = msg2.Int32_values;
            Assert.That(getIntList.Length, Is.EqualTo(2));
            Assert.That(getIntList[0], Is.EqualTo(123));
            Assert.That(getIntList[1], Is.EqualTo(456));

            string[] setStringSequence = new string[2];
            setStringSequence[0] = "Hello";
            setStringSequence[1] = "world";
            test_msgs.msg.BoundedSequences msg3 = new test_msgs.msg.BoundedSequences();
            msg3.String_values = setStringSequence;
            string[] getStringSequence = msg3.String_values;
            Assert.That(getStringSequence.Length, Is.EqualTo(2));
            Assert.That(getStringSequence[0], Is.EqualTo("Hello"));
            Assert.That(getStringSequence[1], Is.EqualTo("world"));
        }

        [Test]
        public void SetNested()
        {
            test_msgs.msg.Nested msg = new test_msgs.msg.Nested();
            test_msgs.msg.BasicTypes basic_types_msg = msg.Basic_types_value;
            Assert.That(basic_types_msg.Int32_value, Is.EqualTo(0));
            basic_types_msg.Int32_value = 25;
            Assert.That(basic_types_msg.Int32_value, Is.EqualTo(25));
            test_msgs.msg.BasicTypes basic_types_msg2 = msg.Basic_types_value;
            Assert.That(basic_types_msg2.Int32_value, Is.EqualTo(25));
        }

        [Test]
        public void SetMultiNested()
        {
            test_msgs.msg.MultiNested msg = new test_msgs.msg.MultiNested();

            msg.Unbounded_sequence_of_unbounded_sequences = new test_msgs.msg.UnboundedSequences[3];
            var setUnboundedSequences = new test_msgs.msg.UnboundedSequences();
            string[] string_array = new string[2];
            setUnboundedSequences.String_values = string_array;
            setUnboundedSequences.String_values[0] = "hello";

            msg.Unbounded_sequence_of_unbounded_sequences[0] = setUnboundedSequences;
            msg.Unbounded_sequence_of_unbounded_sequences[0].String_values[1] = "world";

            Assert.That(msg.Unbounded_sequence_of_unbounded_sequences.Length, Is.EqualTo(3));

            var getUnboundedOfUnbounded = msg.Unbounded_sequence_of_unbounded_sequences;

            Assert.That(getUnboundedOfUnbounded[0].String_values[0], Is.EqualTo("hello"));
            Assert.That(getUnboundedOfUnbounded[0].String_values[1], Is.EqualTo("world"));
        }
    }
}
