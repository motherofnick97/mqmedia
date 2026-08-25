import { ChangeDetectorRef, Component, Injector, ViewChild } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { PagedListingComponentBase } from 'shared/paged-listing-component-base';
import {
    ContractServiceProxy,
    ContractDto,
    ContractStatus,
    KolDuplicateContractServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { forkJoin } from 'rxjs';
import { CreateContractDialogComponent } from './create-contract/create-contract-dialog.component';
import { EditContractDialogComponent } from './edit-contract/edit-contract-dialog.component';
import { ManageContractKolsDialogComponent } from './contract-kols/manage-contract-kols-dialog.component';
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

@Component({
    templateUrl: './contracts.component.html',
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
    ],
})
export class ContractsComponent extends PagedListingComponentBase<ContractDto> {
    @ViewChild('dataTable', { static: true }) dataTable: Table;
    @ViewChild('paginator', { static: true }) paginator: Paginator;

    keyword = '';
    filterStatus: ContractStatus | undefined = undefined;
    advancedFiltersVisible = false;

    readonly contractStatusComplete = ContractStatus.Complete;

    // contractId -> tên các contract không cho trùng KOL
    duplicateMap: Record<string, string[]> = {};
    private _allContractNames: Record<string, string> = {};

    contractStatuses = [
        { value: undefined, label: 'Tất cả' },
        { value: ContractStatus.Prepare, label: 'Chuẩn bị' },
        { value: ContractStatus.Processing, label: 'Đang triển khai' },
        { value: ContractStatus.Complete, label: 'Hoàn thành' },
        { value: ContractStatus.Pause, label: 'Tạm dừng' },
        { value: ContractStatus.Cancel, label: 'Hủy' },
    ];

    constructor(
        injector: Injector,
        private _contractService: ContractServiceProxy,
        private _kolDuplicateContractService: KolDuplicateContractServiceProxy,
        private _modalService: BsModalService,
        cd: ChangeDetectorRef
    ) {
        super(injector, cd);
    }

    createContract(): void {
        const modalRef: BsModalRef = this._modalService.show(CreateContractDialogComponent, { class: 'modal-lg' });
        modalRef.content.onSave.subscribe(() => this.refresh());
    }

    editContract(contract: ContractDto): void {
        if (contract.status === this.contractStatusComplete) {
            abp.notify.warn('Hợp đồng đã hoàn thành, không thể chỉnh sửa.');
            return;
        }
        const modalRef: BsModalRef = this._modalService.show(EditContractDialogComponent, {
            class: 'modal-lg',
            initialState: { id: contract.id },
        });
        modalRef.content.onSave.subscribe(() => this.refresh());
    }

    clearFilters(): void {
        this.keyword = '';
        this.filterStatus = undefined;
        this.list();
    }

    getStatusSeverity(status: ContractStatus): 'secondary' | 'info' | 'success' | 'warn' | 'danger' {
        const map: Record<number, 'secondary' | 'info' | 'success' | 'warn' | 'danger'> = {
            [ContractStatus.Prepare]: 'secondary',
            [ContractStatus.Processing]: 'info',
            [ContractStatus.Complete]: 'success',
            [ContractStatus.Pause]: 'warn',
            [ContractStatus.Cancel]: 'danger',
        };
        return map[status] ?? 'secondary';
    }

    getStatusLabel(status: ContractStatus): string {
        const map: Record<number, string> = {
            [ContractStatus.Prepare]: 'Chuẩn bị',
            [ContractStatus.Processing]: 'Đang triển khai',
            [ContractStatus.Complete]: 'Hoàn thành',
            [ContractStatus.Pause]: 'Tạm dừng',
            [ContractStatus.Cancel]: 'Hủy',
        };
        return map[status] ?? '';
    }

    list(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator.changePage(0);
            if (this.primengTableHelper.records && this.primengTableHelper.records.length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        forkJoin({
            contracts: this._contractService.getAll(
                this.keyword,
                this.filterStatus,
                undefined,
                this.primengTableHelper.getSorting(this.dataTable),
                this.primengTableHelper.getSkipCount(this.paginator, event),
                this.primengTableHelper.getMaxResultCount(this.paginator, event)
            ),
            allContracts: this._contractService.getAll(undefined, undefined, undefined, undefined, 0, 1000),
            duplicates: this._kolDuplicateContractService.getAll(undefined, undefined, 0, 1000),
        })
        .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
        .subscribe(({ contracts, allContracts, duplicates }) => {
            this.primengTableHelper.records = contracts.items ?? [];
            this.primengTableHelper.totalRecordsCount = contracts.totalCount;

            this._allContractNames = {};
            for (const c of allContracts.items ?? []) {
                this._allContractNames[c.id] = c.name ?? '';
            }

            this.duplicateMap = {};
            for (const d of duplicates.items ?? []) {
                const a = d.firstContractId;
                const b = d.secondContractId;
                if (!a || !b) continue;
                if (!this.duplicateMap[a]) this.duplicateMap[a] = [];
                if (!this.duplicateMap[b]) this.duplicateMap[b] = [];
                this.duplicateMap[a].push(this._allContractNames[b] ?? b);
                this.duplicateMap[b].push(this._allContractNames[a] ?? a);
            }

            this.primengTableHelper.hideLoadingIndicator();
            this.cd.detectChanges();
        });
    }

    manageKols(contract: ContractDto): void {
        this._modalService.show(ManageContractKolsDialogComponent, {
            class: 'modal-lg',
            initialState: { contractId: contract.id, contractName: contract.name ?? '', contractStatus: contract.status },
        });
    }

    delete(contract: ContractDto): void {
        abp.message.confirm(
            this.l('ContractDeleteWarningMessage', contract.name),
            undefined,
            (result: boolean) => {
                if (result) {
                    this._contractService.delete(contract.id).subscribe(() => {
                        abp.notify.success(this.l('SuccessfullyDeleted'));
                        this.refresh();
                    });
                }
            }
        );
    }
}
