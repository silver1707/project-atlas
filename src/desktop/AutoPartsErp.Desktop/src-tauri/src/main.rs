#[tauri::command]
fn save_session(payload: String) -> Result<(), String> {
    let entry = keyring::Entry::new("AutoPartsErp", "local-session").map_err(|err| err.to_string())?;
    entry
        .set_password(&payload)
        .map_err(|err| err.to_string())
}

#[tauri::command]
fn load_session() -> Result<Option<String>, String> {
    let entry = keyring::Entry::new("AutoPartsErp", "local-session").map_err(|err| err.to_string())?;
    match entry.get_password() {
        Ok(value) => Ok(Some(value)),
        Err(keyring::Error::NoEntry) => Ok(None),
        Err(err) => Err(err.to_string()),
    }
}

#[tauri::command]
fn clear_session() -> Result<(), String> {
    let entry = keyring::Entry::new("AutoPartsErp", "local-session").map_err(|err| err.to_string())?;
    match entry.delete_credential() {
        Ok(_) | Err(keyring::Error::NoEntry) => Ok(()),
        Err(err) => Err(err.to_string()),
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .invoke_handler(tauri::generate_handler![save_session, load_session, clear_session])
        .run(tauri::generate_context!())
        .expect("error while running AutoParts ERP desktop");
}

fn main() {
    run();
}
