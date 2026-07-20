export function buildDownloadFileName(name: string | undefined, storedFilePath: string | undefined): string {
    const fallback = storedFilePath ?? 'file';
    if (!name) return fallback;

    const dotIndex = storedFilePath?.lastIndexOf('.') ?? -1;
    const ext = dotIndex >= 0 ? storedFilePath!.substring(dotIndex) : '';

    return name.toLowerCase().endsWith(ext.toLowerCase()) ? name : name + ext;
}
