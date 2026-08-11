import os
import re
import json
import subprocess
import tempfile
import urllib.request
import zipfile

def get_latest_version(existing_ver_str):
    try:
        ver = float(existing_ver_str)
        return f"{ver + 0.1:.1f}"
    except ValueError:
        return "1.1"

def parse_filename(filename):
    basename = os.path.splitext(filename)[0]
    m = re.match(r'^(ic_fluent_.+)_(\d+)_(regular|filled|color)$', basename, re.IGNORECASE)
    if m:
        return m.group(1), m.group(2), m.group(3).lower(), basename
    return None

def fetch_assets_sparse(target_dir):
    """
    Uses Git Sparse-Checkout to download ONLY the 'assets' subfolder.
    Reduces download from ~1.1 GB to ~70 MB in ~2 seconds.
    """
    print("Performing Git Sparse-Checkout to fetch ONLY the 'assets' directory...")
    try:
        subprocess.run(["git", "clone", "--depth", "1", "--no-checkout", "https://github.com/microsoft/fluentui-system-icons.git", target_dir], check=True)
        subprocess.run(["git", "sparse-checkout", "init", "--cone"], cwd=target_dir, check=True)
        subprocess.run(["git", "sparse-checkout", "set", "assets"], cwd=target_dir, check=True)
        subprocess.run(["git", "checkout"], cwd=target_dir, check=True)
        assets_path = os.path.join(target_dir, "assets")
        if os.path.exists(assets_path):
            return assets_path
    except Exception as e:
        print(f"Git Sparse-Checkout error: {e}")
    return None

def main():
    target_json_path = os.path.join('FluentDeck', 'FluentDeck', 'Assets', 'data', 'icon_metadata.json')
    
    current_version = "1.0"
    existing_icons = []
    
    if os.path.exists(target_json_path):
        with open(target_json_path, 'r', encoding='utf-8') as f:
            try:
                data = json.load(f)
                current_version = data.get('version', '1.0')
                existing_icons = data.get('icons', [])
            except Exception as e:
                print(f"Error reading existing json: {e}")

    metaphors_map = {}
    for icon in existing_icons:
        if 'name' in icon and 'metaphor' in icon:
            metaphors_map[icon['name'].lower()] = icon.get('metaphor', [])

    repo_root = None
    
    # 1. Check if user has a local clone already (e.g. fluentui-system-icons/assets)
    local_check = os.path.join('fluentui-system-icons', 'assets')
    if os.path.exists(local_check):
        print(f"Found local icons assets folder at '{local_check}'. Skipping download...")
        repo_root = local_check

    with tempfile.TemporaryDirectory() as temp_dir:
        if not repo_root:
            # 2. Try fast sparse checkout (only downloads assets/ folder ~70MB)
            sparse_dir = os.path.join(temp_dir, "fluent_sparse")
            repo_root = fetch_assets_sparse(sparse_dir)

        if not repo_root:
            # 3. Fallback to zip download if git command fails
            print("Downloading main zip fallback...")
            zip_url = "https://github.com/microsoft/fluentui-system-icons/archive/refs/heads/main.zip"
            zip_path = os.path.join(temp_dir, "icons.zip")
            urllib.request.urlretrieve(zip_url, zip_path)
            
            with zipfile.ZipFile(zip_path, 'r') as zip_ref:
                zip_ref.extractall(temp_dir)
                
            for root, dirs, files in os.walk(temp_dir):
                if 'assets' in dirs:
                    repo_root = os.path.join(root, 'assets')
                    break

        if not repo_root or not os.path.exists(repo_root):
            print("Error: Could not locate assets directory.")
            return

        print(f"Scanning icons assets at '{repo_root}'...")
        
        icons_dict = {}

        for item_name in os.listdir(repo_root):
            item_path = os.path.join(repo_root, item_name)
            if not os.path.isdir(item_path):
                continue
            
            display_name = item_name.strip()
            svg_dir = os.path.join(item_path, 'SVG')
            if not os.path.exists(svg_dir):
                svg_dir = item_path
                
            for root_dir, _, files in os.walk(svg_dir):
                for f in files:
                    if f.lower().endswith('.svg'):
                        parsed = parse_filename(f)
                        if parsed:
                            _, size, style, fname = parsed
                            if display_name not in icons_dict:
                                icons_dict[display_name] = {
                                    'regular': {},
                                    'filled': {},
                                    'color': {}
                                }
                            icons_dict[display_name][style][size] = fname

        if not icons_dict:
            print("No icons found in assets.")
            return

        output_icons_list = []
        for display_name in sorted(icons_dict.keys(), key=lambda s: s.lower()):
            icon_data = icons_dict[display_name]
            meta = metaphors_map.get(display_name.lower(), [])
            
            output_icons_list.append({
                "name": display_name,
                "regular": dict(sorted(icon_data['regular'].items(), key=lambda x: int(x[0]) if x[0].isdigit() else 99)),
                "filled": dict(sorted(icon_data['filled'].items(), key=lambda x: int(x[0]) if x[0].isdigit() else 99)),
                "color": dict(sorted(icon_data['color'].items(), key=lambda x: int(x[0]) if x[0].isdigit() else 99)),
                "metaphor": meta
            })

        total_count = len(output_icons_list)
        total_regular = sum(1 for item in output_icons_list if len(item['regular']) > 0)
        total_filled = sum(1 for item in output_icons_list if len(item['filled']) > 0)
        total_color = sum(1 for item in output_icons_list if len(item['color']) > 0)

        # Only write file and bump version if icons metadata actually changed
        has_changed = (output_icons_list != existing_icons)

        if not has_changed and os.path.exists(target_json_path):
            print(f"No changes detected in icon assets. Skipping JSON file rewrite (Version {current_version}).")
            print(f"Stats: Total Icons={total_count}, Regular={total_regular}, Filled={total_filled}, Color={total_color}")
            if os.path.exists('.commit_msg.txt'):
                os.remove('.commit_msg.txt')
            return

        final_version = get_latest_version(current_version) if os.path.exists(target_json_path) else current_version
        print(f"Changes detected in icon assets. Bumping version from {current_version} to {final_version}...")

        # Calculate added and removed icon names for commit message
        p_names = set(i['name'] for i in existing_icons)
        c_names = set(i['name'] for i in output_icons_list)
        added = sorted(list(c_names - p_names))
        removed = sorted(list(p_names - c_names))

        msg_lines = [f"Auto-sync icon_metadata.json (v{final_version})"]
        details = []
        if added:
            details.append(f"Added ({len(added)}): " + ", ".join(added[:15]) + ("..." if len(added) > 15 else ""))
        if removed:
            details.append(f"Removed ({len(removed)}): " + ", ".join(removed[:15]) + ("..." if len(removed) > 15 else ""))

        if details:
            msg_lines.append("")
            msg_lines.extend(details)

        commit_msg = "\n".join(msg_lines)
        with open('.commit_msg.txt', 'w', encoding='utf-8') as f:
            f.write(commit_msg)

        output_data = {
            "version": final_version,
            "totalRegular": total_regular,
            "totalFilled": total_filled,
            "totalColor": total_color,
            "icons": output_icons_list
        }

        os.makedirs(os.path.dirname(target_json_path), exist_ok=True)
        with open(target_json_path, 'w', encoding='utf-8') as f:
            json.dump(output_data, f, indent=2)

        print(f"Successfully generated {target_json_path} (Version {final_version})")
        print(f"Stats: Total Icons={total_count}, Regular={total_regular}, Filled={total_filled}, Color={total_color}")

if __name__ == '__main__':
    main()
