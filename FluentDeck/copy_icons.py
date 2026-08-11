import os
import shutil
import json

src_root = r"E:\website\WinUI-3-Apps-List\fluentui-system-icons\assets"
dst_root = r"E:\website\WinUI-3-Apps-List\FluentDeck\FluentDeck\Assets"

dir_regular = os.path.join(dst_root, "icons", "icon_regular")
dir_filled = os.path.join(dst_root, "icons", "icon_filled")
dir_color = os.path.join(dst_root, "icons", "icon_color")
data_dir = os.path.join(dst_root, "data")
json_path = os.path.join(data_dir, "icon_metadata.json")

os.makedirs(dir_regular, exist_ok=True)
os.makedirs(dir_filled, exist_ok=True)
os.makedirs(dir_color, exist_ok=True)
os.makedirs(data_dir, exist_ok=True)

icon_folders = [f for f in os.listdir(src_root) if os.path.isdir(os.path.join(src_root, f))]
print(f"Found {len(icon_folders)} icon asset folders.")

icons_metadata_list = []
copied_svg_count = 0

for folder_name in sorted(icon_folders):
    folder_path = os.path.join(src_root, folder_name)
    meta_file = os.path.join(folder_path, "metadata.json")
    svg_dir = os.path.join(folder_path, "SVG")

    icon_name = folder_name
    metaphor = []

    if os.path.exists(meta_file):
        try:
            with open(meta_file, "r", encoding="utf-8") as f:
                meta = json.load(f)
                icon_name = meta.get("name", folder_name)
                m = meta.get("metaphor", [])
                if isinstance(m, list):
                    metaphor = [str(x) for x in m if x is not None]
                elif isinstance(m, str):
                    metaphor = [m]
        except Exception as e:
            pass

    regular_map = {}
    filled_map = {}
    color_map = {}

    if os.path.exists(svg_dir):
        svg_files = [f for f in os.listdir(svg_dir) if f.lower().endswith(".svg")]
        for filename in svg_files:
            src_svg = os.path.join(svg_dir, filename)
            base_name = filename[:-4]  # drop .svg

            parts = base_name.split("_")
            size_idx = -1
            for i in range(len(parts) - 1, -1, -1):
                if parts[i].isdigit():
                    size_idx = i
                    break

            if size_idx == -1:
                continue

            size_str = parts[size_idx]
            style_part = "_".join(parts[size_idx + 1:]).lower()

            target_dir = None
            if style_part.startswith("filled"):
                target_dir = dir_filled
                filled_map[size_str] = base_name
            elif style_part.startswith("regular") or style_part == "light":
                target_dir = dir_regular
                regular_map[size_str] = base_name
            elif style_part.startswith("color"):
                target_dir = dir_color
                color_map[size_str] = base_name

            if target_dir:
                dst_svg = os.path.join(target_dir, filename)
                shutil.copy2(src_svg, dst_svg)
                copied_svg_count += 1

    if regular_map or filled_map or color_map:
        icons_metadata_list.append({
            "name": icon_name,
            "regular": regular_map,
            "filled": filled_map,
            "color": color_map,
            "metaphor": metaphor
        })

output_data = {"icons": icons_metadata_list}

with open(json_path, "w", encoding="utf-8") as f:
    json.dump(output_data, f, indent=2, ensure_ascii=False)

print(f"Copy complete! Copied {copied_svg_count} SVG files across regular, filled, and color folders.")
print(f"Generated {json_path} with {len(icons_metadata_list)} icons.")
