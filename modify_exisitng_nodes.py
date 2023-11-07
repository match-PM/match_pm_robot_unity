import re

def modify_c_code_in_file(file_path, node_ids, **kwargs):
    with open(file_path, 'r') as file:
        c_code = file.read()

    for id in node_ids:
        node_id = f"UA_NODEID_NUMERIC(ns[1], {id}LU)"
        # Find NodeId in C-Code
        node_id_pattern = re.compile(re.escape(node_id) + r',')
        match = node_id_pattern.search(c_code)

        if match:
            start, end = match.span()

            # Backward search for code block start 
            code_block_start = c_code.rfind("static UA_StatusCode", 0, start)

            if code_block_start >= 0:
                for key, value in kwargs.items():
                    code_block = c_code[code_block_start:end]
                    # Change die values
                    modified_code = re.sub(f'({key} = \d+;)', f'{key} = {value};', code_block)

                    # Change modifies code in entire C-code
                    c_code = c_code[:code_block_start] + modified_code + c_code[end:]

    with open(file_path, 'w') as file:
        file.write(c_code)

file_path = '/home/pmlab/pm_ros2_ws/src/match_pm_robot/opcua_server/src/pm_opcua_server.c'
ids = [50422, 50427, 50398, 50397, 50388, 50387, 50393, 50392, 50378, 50377, 50383, 50382, 50403, 50402, 50439, 50305, 50310, 50321, 50326, 50294, 50289, 50337, 50342, 50353, 50358, 50241, 50246, 50257, 50262, 50273, 50278]
attr_to_change = {'attr.userAccessLevel': 3, 'attr.accessLevel': 3}
modify_c_code_in_file(file_path, ids, **attr_to_change)
