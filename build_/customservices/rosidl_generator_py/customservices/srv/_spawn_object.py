# generated from rosidl_generator_py/resource/_idl.py.em
# with input from customservices:srv/SpawnObject.idl
# generated code does not contain a copyright notice


# Import statements for member types

import builtins  # noqa: E402, I100

import math  # noqa: E402, I100

# Member 'translation'
# Member 'rotation'
import numpy  # noqa: E402, I100

import rosidl_parser.definition  # noqa: E402, I100


class Metaclass_SpawnObject_Request(type):
    """Metaclass of message 'SpawnObject_Request'."""

    _CREATE_ROS_MESSAGE = None
    _CONVERT_FROM_PY = None
    _CONVERT_TO_PY = None
    _DESTROY_ROS_MESSAGE = None
    _TYPE_SUPPORT = None

    __constants = {
    }

    @classmethod
    def __import_type_support__(cls):
        try:
            from rosidl_generator_py import import_type_support
            module = import_type_support('customservices')
        except ImportError:
            import logging
            import traceback
            logger = logging.getLogger(
                'customservices.srv.SpawnObject_Request')
            logger.debug(
                'Failed to import needed modules for type support:\n' +
                traceback.format_exc())
        else:
            cls._CREATE_ROS_MESSAGE = module.create_ros_message_msg__srv__spawn_object__request
            cls._CONVERT_FROM_PY = module.convert_from_py_msg__srv__spawn_object__request
            cls._CONVERT_TO_PY = module.convert_to_py_msg__srv__spawn_object__request
            cls._TYPE_SUPPORT = module.type_support_msg__srv__spawn_object__request
            cls._DESTROY_ROS_MESSAGE = module.destroy_ros_message_msg__srv__spawn_object__request

    @classmethod
    def __prepare__(cls, name, bases, **kwargs):
        # list constant names here so that they appear in the help text of
        # the message class under "Data and other attributes defined here:"
        # as well as populate each message instance
        return {
        }


class SpawnObject_Request(metaclass=Metaclass_SpawnObject_Request):
    """Message class 'SpawnObject_Request'."""

    __slots__ = [
        '_obj_name',
        '_parent_frame',
        '_translation',
        '_rotation',
        '_cad_data',
    ]

    _fields_and_field_types = {
        'obj_name': 'string',
        'parent_frame': 'string',
        'translation': 'float[3]',
        'rotation': 'float[4]',
        'cad_data': 'string',
    }

    SLOT_TYPES = (
        rosidl_parser.definition.UnboundedString(),  # noqa: E501
        rosidl_parser.definition.UnboundedString(),  # noqa: E501
        rosidl_parser.definition.Array(rosidl_parser.definition.BasicType('float'), 3),  # noqa: E501
        rosidl_parser.definition.Array(rosidl_parser.definition.BasicType('float'), 4),  # noqa: E501
        rosidl_parser.definition.UnboundedString(),  # noqa: E501
    )

    def __init__(self, **kwargs):
        assert all('_' + key in self.__slots__ for key in kwargs.keys()), \
            'Invalid arguments passed to constructor: %s' % \
            ', '.join(sorted(k for k in kwargs.keys() if '_' + k not in self.__slots__))
        self.obj_name = kwargs.get('obj_name', str())
        self.parent_frame = kwargs.get('parent_frame', str())
        if 'translation' not in kwargs:
            self.translation = numpy.zeros(3, dtype=numpy.float32)
        else:
            self.translation = numpy.array(kwargs.get('translation'), dtype=numpy.float32)
            assert self.translation.shape == (3, )
        if 'rotation' not in kwargs:
            self.rotation = numpy.zeros(4, dtype=numpy.float32)
        else:
            self.rotation = numpy.array(kwargs.get('rotation'), dtype=numpy.float32)
            assert self.rotation.shape == (4, )
        self.cad_data = kwargs.get('cad_data', str())

    def __repr__(self):
        typename = self.__class__.__module__.split('.')
        typename.pop()
        typename.append(self.__class__.__name__)
        args = []
        for s, t in zip(self.__slots__, self.SLOT_TYPES):
            field = getattr(self, s)
            fieldstr = repr(field)
            # We use Python array type for fields that can be directly stored
            # in them, and "normal" sequences for everything else.  If it is
            # a type that we store in an array, strip off the 'array' portion.
            if (
                isinstance(t, rosidl_parser.definition.AbstractSequence) and
                isinstance(t.value_type, rosidl_parser.definition.BasicType) and
                t.value_type.typename in ['float', 'double', 'int8', 'uint8', 'int16', 'uint16', 'int32', 'uint32', 'int64', 'uint64']
            ):
                if len(field) == 0:
                    fieldstr = '[]'
                else:
                    assert fieldstr.startswith('array(')
                    prefix = "array('X', "
                    suffix = ')'
                    fieldstr = fieldstr[len(prefix):-len(suffix)]
            args.append(s[1:] + '=' + fieldstr)
        return '%s(%s)' % ('.'.join(typename), ', '.join(args))

    def __eq__(self, other):
        if not isinstance(other, self.__class__):
            return False
        if self.obj_name != other.obj_name:
            return False
        if self.parent_frame != other.parent_frame:
            return False
        if all(self.translation != other.translation):
            return False
        if all(self.rotation != other.rotation):
            return False
        if self.cad_data != other.cad_data:
            return False
        return True

    @classmethod
    def get_fields_and_field_types(cls):
        from copy import copy
        return copy(cls._fields_and_field_types)

    @builtins.property
    def obj_name(self):
        """Message field 'obj_name'."""
        return self._obj_name

    @obj_name.setter
    def obj_name(self, value):
        if __debug__:
            assert \
                isinstance(value, str), \
                "The 'obj_name' field must be of type 'str'"
        self._obj_name = value

    @builtins.property
    def parent_frame(self):
        """Message field 'parent_frame'."""
        return self._parent_frame

    @parent_frame.setter
    def parent_frame(self, value):
        if __debug__:
            assert \
                isinstance(value, str), \
                "The 'parent_frame' field must be of type 'str'"
        self._parent_frame = value

    @builtins.property
    def translation(self):
        """Message field 'translation'."""
        return self._translation

    @translation.setter
    def translation(self, value):
        if isinstance(value, numpy.ndarray):
            assert value.dtype == numpy.float32, \
                "The 'translation' numpy.ndarray() must have the dtype of 'numpy.float32'"
            assert value.size == 3, \
                "The 'translation' numpy.ndarray() must have a size of 3"
            self._translation = value
            return
        if __debug__:
            from collections.abc import Sequence
            from collections.abc import Set
            from collections import UserList
            from collections import UserString
            assert \
                ((isinstance(value, Sequence) or
                  isinstance(value, Set) or
                  isinstance(value, UserList)) and
                 not isinstance(value, str) and
                 not isinstance(value, UserString) and
                 len(value) == 3 and
                 all(isinstance(v, float) for v in value) and
                 all(not (val < -3.402823466e+38 or val > 3.402823466e+38) or math.isinf(val) for val in value)), \
                "The 'translation' field must be a set or sequence with length 3 and each value of type 'float' and each float in [-340282346600000016151267322115014000640.000000, 340282346600000016151267322115014000640.000000]"
        self._translation = numpy.array(value, dtype=numpy.float32)

    @builtins.property
    def rotation(self):
        """Message field 'rotation'."""
        return self._rotation

    @rotation.setter
    def rotation(self, value):
        if isinstance(value, numpy.ndarray):
            assert value.dtype == numpy.float32, \
                "The 'rotation' numpy.ndarray() must have the dtype of 'numpy.float32'"
            assert value.size == 4, \
                "The 'rotation' numpy.ndarray() must have a size of 4"
            self._rotation = value
            return
        if __debug__:
            from collections.abc import Sequence
            from collections.abc import Set
            from collections import UserList
            from collections import UserString
            assert \
                ((isinstance(value, Sequence) or
                  isinstance(value, Set) or
                  isinstance(value, UserList)) and
                 not isinstance(value, str) and
                 not isinstance(value, UserString) and
                 len(value) == 4 and
                 all(isinstance(v, float) for v in value) and
                 all(not (val < -3.402823466e+38 or val > 3.402823466e+38) or math.isinf(val) for val in value)), \
                "The 'rotation' field must be a set or sequence with length 4 and each value of type 'float' and each float in [-340282346600000016151267322115014000640.000000, 340282346600000016151267322115014000640.000000]"
        self._rotation = numpy.array(value, dtype=numpy.float32)

    @builtins.property
    def cad_data(self):
        """Message field 'cad_data'."""
        return self._cad_data

    @cad_data.setter
    def cad_data(self, value):
        if __debug__:
            assert \
                isinstance(value, str), \
                "The 'cad_data' field must be of type 'str'"
        self._cad_data = value


# Import statements for member types

# already imported above
# import builtins

# already imported above
# import rosidl_parser.definition


class Metaclass_SpawnObject_Response(type):
    """Metaclass of message 'SpawnObject_Response'."""

    _CREATE_ROS_MESSAGE = None
    _CONVERT_FROM_PY = None
    _CONVERT_TO_PY = None
    _DESTROY_ROS_MESSAGE = None
    _TYPE_SUPPORT = None

    __constants = {
    }

    @classmethod
    def __import_type_support__(cls):
        try:
            from rosidl_generator_py import import_type_support
            module = import_type_support('customservices')
        except ImportError:
            import logging
            import traceback
            logger = logging.getLogger(
                'customservices.srv.SpawnObject_Response')
            logger.debug(
                'Failed to import needed modules for type support:\n' +
                traceback.format_exc())
        else:
            cls._CREATE_ROS_MESSAGE = module.create_ros_message_msg__srv__spawn_object__response
            cls._CONVERT_FROM_PY = module.convert_from_py_msg__srv__spawn_object__response
            cls._CONVERT_TO_PY = module.convert_to_py_msg__srv__spawn_object__response
            cls._TYPE_SUPPORT = module.type_support_msg__srv__spawn_object__response
            cls._DESTROY_ROS_MESSAGE = module.destroy_ros_message_msg__srv__spawn_object__response

    @classmethod
    def __prepare__(cls, name, bases, **kwargs):
        # list constant names here so that they appear in the help text of
        # the message class under "Data and other attributes defined here:"
        # as well as populate each message instance
        return {
        }


class SpawnObject_Response(metaclass=Metaclass_SpawnObject_Response):
    """Message class 'SpawnObject_Response'."""

    __slots__ = [
        '_success',
    ]

    _fields_and_field_types = {
        'success': 'boolean',
    }

    SLOT_TYPES = (
        rosidl_parser.definition.BasicType('boolean'),  # noqa: E501
    )

    def __init__(self, **kwargs):
        assert all('_' + key in self.__slots__ for key in kwargs.keys()), \
            'Invalid arguments passed to constructor: %s' % \
            ', '.join(sorted(k for k in kwargs.keys() if '_' + k not in self.__slots__))
        self.success = kwargs.get('success', bool())

    def __repr__(self):
        typename = self.__class__.__module__.split('.')
        typename.pop()
        typename.append(self.__class__.__name__)
        args = []
        for s, t in zip(self.__slots__, self.SLOT_TYPES):
            field = getattr(self, s)
            fieldstr = repr(field)
            # We use Python array type for fields that can be directly stored
            # in them, and "normal" sequences for everything else.  If it is
            # a type that we store in an array, strip off the 'array' portion.
            if (
                isinstance(t, rosidl_parser.definition.AbstractSequence) and
                isinstance(t.value_type, rosidl_parser.definition.BasicType) and
                t.value_type.typename in ['float', 'double', 'int8', 'uint8', 'int16', 'uint16', 'int32', 'uint32', 'int64', 'uint64']
            ):
                if len(field) == 0:
                    fieldstr = '[]'
                else:
                    assert fieldstr.startswith('array(')
                    prefix = "array('X', "
                    suffix = ')'
                    fieldstr = fieldstr[len(prefix):-len(suffix)]
            args.append(s[1:] + '=' + fieldstr)
        return '%s(%s)' % ('.'.join(typename), ', '.join(args))

    def __eq__(self, other):
        if not isinstance(other, self.__class__):
            return False
        if self.success != other.success:
            return False
        return True

    @classmethod
    def get_fields_and_field_types(cls):
        from copy import copy
        return copy(cls._fields_and_field_types)

    @builtins.property
    def success(self):
        """Message field 'success'."""
        return self._success

    @success.setter
    def success(self, value):
        if __debug__:
            assert \
                isinstance(value, bool), \
                "The 'success' field must be of type 'bool'"
        self._success = value


class Metaclass_SpawnObject(type):
    """Metaclass of service 'SpawnObject'."""

    _TYPE_SUPPORT = None

    @classmethod
    def __import_type_support__(cls):
        try:
            from rosidl_generator_py import import_type_support
            module = import_type_support('customservices')
        except ImportError:
            import logging
            import traceback
            logger = logging.getLogger(
                'customservices.srv.SpawnObject')
            logger.debug(
                'Failed to import needed modules for type support:\n' +
                traceback.format_exc())
        else:
            cls._TYPE_SUPPORT = module.type_support_srv__srv__spawn_object

            from customservices.srv import _spawn_object
            if _spawn_object.Metaclass_SpawnObject_Request._TYPE_SUPPORT is None:
                _spawn_object.Metaclass_SpawnObject_Request.__import_type_support__()
            if _spawn_object.Metaclass_SpawnObject_Response._TYPE_SUPPORT is None:
                _spawn_object.Metaclass_SpawnObject_Response.__import_type_support__()


class SpawnObject(metaclass=Metaclass_SpawnObject):
    from customservices.srv._spawn_object import SpawnObject_Request as Request
    from customservices.srv._spawn_object import SpawnObject_Response as Response

    def __init__(self):
        raise NotImplementedError('Service classes can not be instantiated')
