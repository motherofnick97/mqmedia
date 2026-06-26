import { ChangeDetectorRef, Component, ElementRef, Injector, ViewChild } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { PagedListingComponentBase } from 'shared/paged-listing-component-base';
import { KolServiceProxy, KolDto, KolDtoPagedResultDto, KolCareer, ChannelType } from '@shared/service-proxies/service-proxies';
import { CreateKolDialogComponent } from './create-kol/create-kol-dialog.component';
import { EditKolDialogComponent } from './edit-kol/edit-kol-dialog.component';
import { Table, TableModule } from 'primeng/table';
import { LazyLoadEvent, PrimeTemplate } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { Tag } from 'primeng/tag';
import { Toolbar } from 'primeng/toolbar';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { HttpClient } from '@angular/common/http';
import { AppConsts } from '@shared/AppConsts';

interface ImportKolError {
    row: number;
    message: string;
}

interface ImportKolResult {
    successCount: number;
    failCount: number;
    errors: ImportKolError[];
}

@Component({
    templateUrl: './kols.component.html',
    animations: [appModuleAnimation()],
    standalone: true,
    imports: [
        FormsModule,
        TableModule,
        PrimeTemplate,
        PaginatorModule,
        LocalizePipe,
        Button,
        Select,
        Tag,
        Toolbar,
        IconField,
        InputIcon,
        InputText,
        CommonModule,
        Dialog,
    ],
})
export class KolsComponent extends PagedListingComponentBase<KolDto> {
    @ViewChild('dataTable', { static: true }) dataTable: Table;
    @ViewChild('paginator', { static: true }) paginator: Paginator;
    @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

    keyword = '';
    filterCareer: KolCareer | undefined = undefined;
    filterChannel: ChannelType | undefined = undefined;
    advancedFiltersVisible = false;

    importLoading = false;
    importResult: ImportKolResult | null = null;
    showImportResult = false;

    careerOptions = [
        { value: undefined, label: 'Tất cả' },
        { value: KolCareer.DuocSi, label: 'Dược sĩ' },
        { value: KolCareer.BacSi, label: 'Bác sĩ' },
        { value: KolCareer.Mom, label: 'Mẹ bé' },
    ];

    channelOptions = [
        { value: undefined, label: 'Tất cả' },
        { value: ChannelType.Tiktok, label: 'TikTok' },
        { value: ChannelType.Facebook, label: 'Facebook' },
    ];

    constructor(
        injector: Injector,
        private _kolService: KolServiceProxy,
        private _modalService: BsModalService,
        private _http: HttpClient,
        cd: ChangeDetectorRef
    ) {
        super(injector, cd);
    }

    createKol(): void {
        const modalRef: BsModalRef = this._modalService.show(CreateKolDialogComponent, { class: 'modal-lg' });
        modalRef.content.onSave.subscribe(() => this.refresh());
    }

    editKol(kol: KolDto): void {
        const modalRef: BsModalRef = this._modalService.show(EditKolDialogComponent, {
            class: 'modal-lg',
            initialState: { id: kol.id },
        });
        modalRef.content.onSave.subscribe(() => this.refresh());
    }

    clearFilters(): void {
        this.keyword = '';
        this.filterCareer = undefined;
        this.filterChannel = undefined;
        this.list();
    }

    openImportDialog(): void {
        this.fileInput.nativeElement.value = '';
        this.fileInput.nativeElement.click();
    }

    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0];
        if (!file) return;

        this.importLoading = true;
        const formData = new FormData();
        formData.append('file', file);

        const url = `${AppConsts.remoteServiceBaseUrl}/api/services/app/Kol/ImportFromExcel`;
        this._http.post<{ result: ImportKolResult }>(url, formData).subscribe({
            next: (response) => {
                this.importLoading = false;
                this.importResult = response.result;
                if (response.result.failCount === 0) {
                    this.notify.success(`Import thành công ${response.result.successCount} KOL`);
                    this.refresh();
                } else {
                    this.showImportResult = true;
                    this.refresh();
                }
                this.cd.detectChanges();
            },
            error: () => {
                this.importLoading = false;
                this.cd.detectChanges();
            },
        });
    }

    closeImportResult(): void {
        this.showImportResult = false;
        this.importResult = null;
    }

    getChannelSeverity(channel: ChannelType): 'danger' | 'info' {
        return channel === ChannelType.Tiktok ? 'danger' : 'info';
    }

    getChannelLabel(channel: ChannelType): string {
        return channel === ChannelType.Tiktok ? 'TikTok' : 'Facebook';
    }

    getCareerLabel(career: KolCareer): string {
        const map = { [KolCareer.DuocSi]: 'Dược sĩ', [KolCareer.BacSi]: 'Bác sĩ', [KolCareer.Mom]: 'Mẹ bé' };
        return map[career] ?? '';
    }

    list(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator.changePage(0);
            if (this.primengTableHelper.records && this.primengTableHelper.records.length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        this._kolService
            .getAll(
                this.keyword,
                this.filterCareer,
                this.filterChannel,
                this.primengTableHelper.getSorting(this.dataTable),
                this.primengTableHelper.getSkipCount(this.paginator, event),
                this.primengTableHelper.getMaxResultCount(this.paginator, event)
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result: KolDtoPagedResultDto) => {
                this.primengTableHelper.records = result.items;
                this.primengTableHelper.totalRecordsCount = result.totalCount;
                this.primengTableHelper.hideLoadingIndicator();
                this.cd.detectChanges();
            });
    }

    delete(kol: KolDto): void {
        abp.message.confirm(
            this.l('KolDeleteWarningMessage', kol.name),
            undefined,
            (result: boolean) => {
                if (result) {
                    this._kolService.delete(kol.id).subscribe(() => {
                        abp.notify.success(this.l('SuccessfullyDeleted'));
                        this.refresh();
                    });
                }
            }
        );
    }
}
