import { ChangeDetectorRef, Component, ElementRef, Injector, ViewChild } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { PagedListingComponentBase } from 'shared/paged-listing-component-base';
import { KolServiceProxy, KolDto, KolDtoPagedResultDto, ChannelType, CareerServiceProxy, CareerDto, ContractServiceProxy, ContractDto } from '@shared/service-proxies/service-proxies';
import { CreateKolDialogComponent } from './create-kol/create-kol-dialog.component';
import { EditKolDialogComponent } from './edit-kol/edit-kol-dialog.component';
import { AddKolToContractsDialogComponent } from './add-to-contracts/add-kol-to-contracts-dialog.component';
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
import { Tooltip } from 'primeng/tooltip';
import { Dialog } from 'primeng/dialog';
import { MultiSelect } from 'primeng/multiselect';
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
        Tooltip,
        CommonModule,
        Dialog,
        MultiSelect,
    ],
})
export class KolsComponent extends PagedListingComponentBase<KolDto> {
    @ViewChild('dataTable', { static: true }) dataTable: Table;
    @ViewChild('paginator', { static: true }) paginator: Paginator;
    @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

    keyword = '';
    filterCareerId: string | undefined = undefined;
    filterChannel: ChannelType | undefined = undefined;
    advancedFiltersVisible = false;

    importLoading = false;
    importResult: ImportKolResult | null = null;
    showImportResult = false;

    showImportDialog = false;
    importCareerIds: string[] = [];
    importContractId: string | null = null;
    pendingImportFile: File | null = null;

    careers: CareerDto[] = [];
    contracts: ContractDto[] = [];

    channelOptions = [
        { value: undefined, label: 'Tất cả' },
        { value: ChannelType.Tiktok, label: 'TikTok' },
        { value: ChannelType.Facebook, label: 'Facebook' },
        { value: ChannelType.Khac, label: 'Khác' },
    ];

    constructor(
        injector: Injector,
        private _kolService: KolServiceProxy,
        private _careerService: CareerServiceProxy,
        private _contractService: ContractServiceProxy,
        private _modalService: BsModalService,
        private _http: HttpClient,
        cd: ChangeDetectorRef
    ) {
        super(injector, cd);
    }

    ngOnInit(): void {
        this._careerService.getAll(undefined, 'Name', 0, 1000).subscribe((r) => {
            this.careers = r.items ?? [];
            this.cd.detectChanges();
        });
        this._contractService.getAll(undefined, undefined, undefined, 'Name', 0, 1000).subscribe((r) => {
            this.contracts = r.items ?? [];
            this.cd.detectChanges();
        });
    }

    createKol(): void {
        const modalRef: BsModalRef = this._modalService.show(CreateKolDialogComponent, { class: 'modal-lg' });
        modalRef.content.onSave.subscribe(() => this.refresh());
    }

    addToContracts(kol: KolDto): void {
        this._modalService.show(AddKolToContractsDialogComponent, {
            class: 'modal-lg',
            initialState: { kolId: kol.id, kolName: kol.name ?? kol.id },
        });
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
        this.filterCareerId = undefined;
        this.filterChannel = undefined;
        this.list();
    }

    openImportDialog(): void {
        this.importCareerIds = [];
        this.importContractId = null;
        this.pendingImportFile = null;
        this.fileInput.nativeElement.value = '';
        this.showImportDialog = true;
    }

    cancelImportDialog(): void {
        this.showImportDialog = false;
        this.pendingImportFile = null;
        this.importCareerIds = [];
        this.importContractId = null;
        this.fileInput.nativeElement.value = '';
    }

    pickImportFile(): void {
        this.fileInput.nativeElement.click();
    }

    downloadTemplate(): void {
        const a = document.createElement('a');
        a.href = '/assets/templates/Danh sách KOL.xlsx';
        a.download = 'Danh sách KOL.xlsx';
        a.click();
    }

    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0];
        if (!file) return;
        this.pendingImportFile = file;
        this.cd.detectChanges();
    }

    submitImport(): void {
        if (!this.pendingImportFile) return;

        this.importLoading = true;
        const formData = new FormData();
        formData.append('file', this.pendingImportFile);
        for (const id of this.importCareerIds) {
            formData.append('CareerIds', id);
        }
        if (this.importContractId) {
            formData.append('ContractId', this.importContractId);
        }

        const url = `${AppConsts.remoteServiceBaseUrl}/api/services/app/Kol/ImportFromExcel`;
        this._http.post<{ result: ImportKolResult }>(url, formData).subscribe({
            next: (response) => {
                this.importLoading = false;
                this.showImportDialog = false;
                this.pendingImportFile = null;
                this.importCareerIds = [];
                this.importContractId = null;
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

    getChannelSeverity(channel: ChannelType): 'danger' | 'info' | 'secondary' {
        if (channel === ChannelType.Tiktok) return 'danger';
        if (channel === ChannelType.Facebook) return 'info';
        return 'secondary';
    }

    getChannelLabel(channel: ChannelType): string {
        if (channel === ChannelType.Tiktok) return 'TikTok';
        if (channel === ChannelType.Facebook) return 'Facebook';
        return 'Khác';
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
                this.filterCareerId,
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
